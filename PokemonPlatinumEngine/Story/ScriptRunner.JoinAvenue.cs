using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>The command of plan 22 · U11: <c>joinavenue</c>, Join Avenue's growth (<see cref="JoinAvenue"/>).</summary>
public sealed partial class ScriptRunner
{
    /// <summary>
    /// <c>visit</c> brings the day's visitors on the first coming of a day once the player manages the avenue (RESULT and
    /// <c>{number}</c> how many came, 0 for none); <c>rank</c> is the avenue's rank (1 to 10), <c>visitors</c> every visitor it
    /// has had and <c>next</c> the visitors still wanted for the next rank (each RESULT and <c>{number}</c>); <c>shop
    /// market|flowers|cafe|stones|vitamins</c> opens that shop with its stock at the rank and waits (RESULT 1), or is 0
    /// where the shop isn't open yet.
    /// </summary>
    private void JoinAvenueCommand(Instruction i)
    {
        var story = host.Story;
        switch (i.Name)
        {
            case "visit":
                Result = lastNumber = JoinAvenue.Visit(story);
                break;
            case "rank":
                Result = lastNumber = JoinAvenue.Rank(story);
                break;
            case "visitors":
                Result = lastNumber = story.Var(JoinAvenue.Vars.Visitors);
                break;
            case "next":
                Result = lastNumber = JoinAvenue.ToNextRank(story.Var(JoinAvenue.Vars.Visitors));
                break;
            case "shop":
            {
                string? stock = JoinAvenue.StockOf(i.Other, JoinAvenue.Rank(story));
                Result = stock == null ? 0 : 1;
                if (stock != null) host.Open(ScriptScreen.Shop, Subject, stock);
                break;
            }
        }
    }
}
