using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// Mom's savings in Johto (plan 43 · LF7; HeartGold's Mom, in our own rules): once asked at home, she keeps a quarter of
/// each trainer battle's prize money won in Johto, gives it back whole when asked, and spends some of it on things she
/// rings to tell of (<c>phone.txt</c>'s <c>MomShops</c>). What she holds is a story variable of ours, so a save keeps it
/// with no field of its own; nothing here draws a number. No drawing or input.
/// </summary>
public static class MomSavings
{
    /// <summary>Set while Mom puts a share of the prize money by (ours).</summary>
    public const string SavingFlag = "FLAG_JOHTO_MOM_SAVING";

    /// <summary>What she holds (ours).</summary>
    public const string Var = "VAR_JOHTO_MOM_SAVINGS";

    /// <summary>The most she holds: the wallet's own most.</summary>
    public const int Most = 999_999;

    /// <summary>Her share of a prize: a quarter, rounded down, in Johto while she is asked to save; nought anywhere else.</summary>
    public static int ShareOf(int prize, string region, StoryState story) =>
        prize > 0 && story.Has(SavingFlag) && string.Equals(region, RegionDatabase.Johto, StringComparison.OrdinalIgnoreCase)
            ? Math.Min(prize / 4, Most - Math.Clamp(story.Var(Var), 0, Most))
            : 0;

    /// <summary>A share put by.</summary>
    public static void Keep(StoryState story, int share)
    {
        if (share <= 0) return;
        story.SetVar(Var, Math.Min(Most, story.Var(Var) + share));
    }
}
