using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// Join Avenue's growth (plan 22 · U11; <c>docs/mechanics/rulings.md</c>, "Join Avenue's growth"), GPU-free. Black 2's avenue
/// fills as passers-by met over the console's wireless come to it and the player, its manager, ranks it up; with no link,
/// ours grows by days: once the player has taken the owner's job (<see cref="ManagerFlag"/>), the first coming to the
/// avenue on a day brings <see cref="VisitorsOn"/> visitors, more the higher its rank, and every <see cref="VisitorsARank"/>
/// visitors raise its rank by one, up to <see cref="TopRank"/>. The visitors open shops in the street's empty bays when the
/// player invites them, as many as the rank has room for (<see cref="Offer"/>, <see cref="RoomAt"/>); every shop rises a level
/// with the money spent in it (<see cref="LevelOf(StoryState, string)"/>) and widens what it offers at its levels
/// (<see cref="MarketAt"/>, <see cref="FlowersAt"/>, <see cref="CafeAt"/>, <see cref="AntiquesAt"/>, <see cref="DojoEffort"/>…);
/// the two shop girls open shops of their own at ranks (<see cref="StonesRank"/>, <see cref="VitaminsRank"/>). Every number is ours. Nothing is saved but the story's own variables and flags
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

        /// <summary>The money spent in a shop (<c>market</c>, <c>dojo</c>…).</summary>
        public static string Spent(string shop) => "VAR_UNOVA_JOIN_AVENUE_SPENT_" + shop.ToUpperInvariant();
    }

    /// <summary>The visitors a rank takes, and the highest rank.</summary>
    public const int VisitorsARank = 10, TopRank = 10;

    /// <summary>The ranks the shop girls' shops open at.</summary>
    public const int StonesRank = 7, VitaminsRank = 9;

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
        CatchUp(story);
        if (!story.Has(ManagerFlag) || story.Has(VisitedTodayFlag)) return 0;
        int came = VisitorsOn(Rank(story));
        story.SetVar(Vars.Visitors, story.Var(Vars.Visitors) + came);
        story.Set(VisitedTodayFlag);
        return came;
    }

    /// <summary>The market's stock at its level (<see cref="LevelOf(StoryState, string)"/>): the first, a second from level <see cref="MarketSecondLevel"/>, a third from <see cref="MarketThirdLevel"/>.</summary>
    public static string MarketAt(int level) =>
        level >= MarketThirdLevel ? "join_avenue_market_3" : level >= MarketSecondLevel ? "join_avenue_market_2" : "join_avenue_market";

    /// <summary>The flower shop's stock at its level.</summary>
    public static string FlowersAt(int level) => level >= SecondStockLevel ? "join_avenue_flowers_2" : "join_avenue_flowers";

    /// <summary>The café's stock at its level.</summary>
    public static string CafeAt(int level) => level >= SecondStockLevel ? "join_avenue_cafe_2" : "join_avenue_cafe";

    /// <summary>The antique shop's stock at its level.</summary>
    public static string AntiquesAt(int level) => level >= AntiquesSecondLevel ? "join_avenue_antiques_2" : "join_avenue_antiques";

    /// <summary>The levels the shops' stock widens at (ours).</summary>
    public const int MarketSecondLevel = 2, MarketThirdLevel = 4, SecondStockLevel = 2, AntiquesSecondLevel = 3;

    /// <summary>
    /// A shop's stock by its name at its own level (<c>market</c>, <c>flowers</c>, <c>cafe</c>, <c>antiques</c>; the shop
    /// girls' <c>stones</c> and <c>vitamins</c>, which have one stock each), whether or not it is open.
    /// </summary>
    public static string? StockAt(string shop, int level) => shop switch
    {
        "market" => MarketAt(level),
        "flowers" => FlowersAt(level),
        "cafe" => CafeAt(level),
        "antiques" => AntiquesAt(level),
        "stones" => "join_avenue_stones",
        "vitamins" => "join_avenue_vitamins",
        _ => null
    };

    /// <summary>A shop's stock as the story stands, at its level, or null where the shop isn't open.</summary>
    public static string? StockOf(StoryState story, string shop) =>
        IsOpen(story, shop) ? StockAt(shop, LevelOf(story, shop)) : null;

    // ------------------------------------------------------------------ the shops' levels (plan 22 · U11, the last pieces)

    /// <summary>
    /// Every shop keeps the money the player has spent in it (<see cref="Vars.Spent"/>), and rises a level, 1 to
    /// <see cref="TopLevel"/>, as it is spent: Black 2's shops level with what their customers spend in them. The raffle,
    /// whose ticket is free, rises with what is spent on the whole avenue; the nursery, which is free too, warms by the
    /// avenue's rank. The thresholds are ours.
    /// </summary>
    public const int TopLevel = 5;

    /// <summary>The shops that sell goods, whose levels go by <see cref="GoodsLevels"/>.</summary>
    public static readonly string[] Shops = { "market", "flowers", "cafe", "antiques", "stones", "vitamins", "dojo", "salon" };

    /// <summary>The money a goods shop's levels 2 to 5 take, a service's (the dojo, the salon), and the raffle's (the whole avenue's).</summary>
    public static readonly int[] GoodsLevels = { 5000, 15000, 30000, 60000 },
        ServiceLevels = { 2000, 5000, 10000, 20000 },
        RaffleLevels = { 10000, 30000, 60000, 100000 };

    /// <summary>The thresholds a shop's levels go by.</summary>
    public static int[] LevelsOf(string shop) => shop switch
    {
        "dojo" or "salon" => ServiceLevels,
        "raffle" => RaffleLevels,
        _ => GoodsLevels
    };

    /// <summary>A shop's level from the money spent in it: 1, and one more for each threshold reached.</summary>
    public static int LevelAt(string shop, int spent)
    {
        int level = 1;
        foreach (int at in LevelsOf(shop))
            if (spent >= at) level++;
        return level;
    }

    /// <summary>The money spent in a shop as the story stands; for the raffle, on the whole avenue.</summary>
    public static int SpentIn(StoryState story, string shop)
    {
        if (shop != "raffle") return Math.Max(0, story.Var(Vars.Spent(shop)));
        long all = 0;
        foreach (string s in Shops) all += Math.Max(0, story.Var(Vars.Spent(s)));
        return (int)Math.Min(int.MaxValue, all);
    }

    /// <summary>A shop's level as the story stands.</summary>
    public static int LevelOf(StoryState story, string shop) => LevelAt(shop, SpentIn(story, shop));

    /// <summary>The money still wanted for a shop's next level; 0 at the top.</summary>
    public static int ToNextLevel(StoryState story, string shop)
    {
        int spent = SpentIn(story, shop);
        foreach (int at in LevelsOf(shop))
            if (spent < at) return at - spent;
        return 0;
    }

    /// <summary>
    /// What was spent in a shop between the money in hand as it was come to (<paramref name="till"/>, which the script's
    /// runner notes and the story never keeps) and now: the money in hand fallen by is added to the shop's own (a sale to the
    /// shop that leaves more money in hand adds nothing, and takes nothing away). Returns what was added.
    /// </summary>
    public static int Tally(StoryState story, string shop, int till, int money)
    {
        int spent = Math.Max(0, till - Math.Max(0, money));
        if (spent > 0) story.SetVar(Vars.Spent(shop), (int)Math.Min(int.MaxValue, (long)Math.Max(0, story.Var(Vars.Spent(shop))) + spent));
        return spent;
    }

    // ------------------------------------------------------------------ the other shops (plan 22 · U11's leftovers)

    /// <summary>
    /// The avenue's other shops (rulings, "Join Avenue's other shops"), each opened in an empty bay of the street by a visitor
    /// the player invites (<see cref="Offer"/>; Black 2's passers-by settle in shops of their own): the raffle, the dojo, the
    /// salon, the nursery and the antique shop. A shop opened is the story's flag (<see cref="OpenFlag"/>), set by the
    /// visitor's script, which also brings its keeper behind its counter.
    /// </summary>
    public static readonly string[] Invited = { "raffle", "dojo", "salon", "nursery", "antiques" };

    /// <summary>The flag a shop of <see cref="Invited"/> is open by.</summary>
    public static string OpenFlag(string shop) => "FLAG_UNOVA_JOIN_AVENUE_" + shop.ToUpperInvariant() + "_OPEN";

    /// <summary>
    /// The flag of an avenue whose shops open by invitation: the owner sets it with the manager's job. A save that took the
    /// job before it (when the shops opened by the avenue's rank alone) is given, on its next coming, the shops it had open
    /// then (<see cref="CatchUp"/>).
    /// </summary>
    public const string InvitationsFlag = "FLAG_UNOVA_JOIN_AVENUE_INVITATIONS";

    /// <summary>The ranks the five shops opened at before invitations, which <see cref="CatchUp"/> still reads.</summary>
    public const int RaffleRank = 1, DojoRank = 2, SalonRank = 3, NurseryRank = 4, AntiquesRank = 5;

    /// <summary>The rank a shop opened at by the avenue's rank alone (the shop girls' still do).</summary>
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

    /// <summary>How many of the invited shops the avenue has room for at a rank: one at rank 1, one more a rank, all five from 5.</summary>
    public static int RoomAt(int rank) => Math.Clamp(rank, 1, Invited.Length);

    /// <summary>How many of the invited shops are open.</summary>
    public static int InvitedOpen(StoryState story)
    {
        int open = 0;
        foreach (string shop in Invited)
            if (story.Has(OpenFlag(shop))) open++;
        return open;
    }

    /// <summary>
    /// Whether a shop is open as the story stands: the market, the flower shop and the café always; an invited shop once its
    /// flag is set; the shop girls' at their ranks.
    /// </summary>
    public static bool IsOpen(StoryState story, string shop) => shop switch
    {
        "market" or "flowers" or "cafe" => true,
        "stones" or "vitamins" => Rank(story) >= OpensAt(shop),
        _ => Array.IndexOf(Invited, shop) >= 0 && story.Has(OpenFlag(shop))
    };

    /// <summary>
    /// What each of the <see cref="VisitorList"/> visitors would open (an index of <see cref="Invited"/>), ours: no one offers
    /// the shop they speak of as a customer in their greeting.
    /// </summary>
    private static readonly int[] Offers = { 0, 2, 1, 3, 4, 0, 2, 1, 3, 0, 4, 2, 1, 0, 3, 4 };

    /// <summary>The shop a visitor (0 on) would open.</summary>
    public static string OfferOf(int visitor) => Invited[Offers[((visitor % VisitorList) + VisitorList) % VisitorList]];

    /// <summary>What a visitor's offer comes to: <see cref="NoOffer"/> where their shop is open already, <see cref="NoRoom"/> where the avenue's rank has no room for another, else 1 to 5, the shop (<see cref="Invited"/>) they would open.</summary>
    public const int NoOffer = 0, NoRoom = 9;

    /// <summary>The offer of the visitor standing at a place (1 to <see cref="VisitorsStanding"/>) as the story stands.</summary>
    public static int Offer(StoryState story, int place)
    {
        string shop = OfferOf(VisitorAt(place, story.Var(Vars.Visitors)));
        if (story.Has(OpenFlag(shop))) return NoOffer;
        if (InvitedOpen(story) >= RoomAt(Rank(story))) return NoRoom;
        return Array.IndexOf(Invited, shop) + 1;
    }

    /// <summary>
    /// A managed avenue whose shops opened by its rank (a save from before invitations) keeps the shops it had open then, and
    /// opens by invitation from now on. Returns how many were opened.
    /// </summary>
    public static int CatchUp(StoryState story)
    {
        if (!story.Has(ManagerFlag) || story.Has(InvitationsFlag)) return 0;
        int rank = Rank(story), opened = 0;
        foreach (string shop in Invited)
            if (rank >= OpensAt(shop) && !story.Has(OpenFlag(shop))) { story.Set(OpenFlag(shop)); opened++; }
        // and each shop keeps what the rank gave it then, as spending of the level that gives as much now
        SpentAtLeast(story, "market", rank >= 6 ? 4 : rank >= 3 ? 2 : 1);
        SpentAtLeast(story, "flowers", rank >= 4 ? 2 : 1);
        SpentAtLeast(story, "cafe", rank >= 5 ? 2 : 1);
        SpentAtLeast(story, "antiques", rank >= 8 ? 3 : 1);
        if (rank >= DojoRank) SpentAtLeast(story, "dojo", (4 + 2 * (rank - DojoRank)) / 4);
        if (rank >= SalonRank) SpentAtLeast(story, "salon", 1 + 3 * (rank - SalonRank) / 5);
        story.Set(InvitationsFlag);
        return opened;
    }

    /// <summary>Raises what was spent in a shop to what its level takes, where it is less.</summary>
    private static void SpentAtLeast(StoryState story, string shop, int level)
    {
        level = Math.Clamp(level, 1, TopLevel);
        if (level < 2) return;
        int at = LevelsOf(shop)[level - 2];
        if (story.Var(Vars.Spent(shop)) < at) story.SetVar(Vars.Spent(shop), at);
    }

    /// <summary>What a service costs: the dojo's session, the salon's treatment (the nursery and the raffle are free).</summary>
    public const int DojoPrice = 1000, SalonPrice = 500;

    /// <summary>The dojo's session at its level: so much effort in the stat trained, four at level 1 and four more a level (20 at the top).</summary>
    public static int DojoEffort(int level) => 4 * Math.Clamp(level, 1, TopLevel);

    /// <summary>
    /// A dojo session: the Pokémon gains <see cref="DojoEffort"/> in the stat, cut to the rules' cap of a stat and to 510 in
    /// all, and its stats are worked out again. Returns what it gained (0 when it can gain no more there, or for an Egg).
    /// </summary>
    public static int Train(Pokemon pokemon, StatType stat, int level, Data.Ruleset? rules = null)
    {
        if (pokemon.IsEgg) return 0;
        int statCap = (rules ?? Data.Ruleset.Current).EvStatCap;
        int now = EffortRules.Get(pokemon, stat);
        int gain = Math.Min(DojoEffort(level), Math.Min(statCap - now, EffortRules.MaxTotal - EffortRules.Total(pokemon)));
        if (gain <= 0) return 0;
        EffortRules.Set(pokemon, stat, now + gain);
        pokemon.RecalculateStats();
        return gain;
    }

    /// <summary>The salon's treatment at its level: so much friendship, ten at level 1 and five more a level (30 at the top).</summary>
    public static int SalonFriendship(int level) => 10 + 5 * (Math.Clamp(level, 1, TopLevel) - 1);

    /// <summary>A salon treatment: the Pokémon grows friendlier, never past 255. Returns how much (0 for an Egg or one at the top).</summary>
    public static int Groom(Pokemon pokemon, int level)
    {
        if (pokemon.IsEgg) return 0;
        int before = pokemon.Friendship;
        pokemon.Friendship = Math.Min(255, before + SalonFriendship(level));
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

    /// <summary>The raffle's prize for a draw of 0 to 99 at its level, which moves the draw up by six a level.</summary>
    public static string RafflePrize(int draw, int level)
    {
        int at = Math.Clamp(draw, 0, 99) + 6 * (Math.Clamp(level, 1, TopLevel) - 1);
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
