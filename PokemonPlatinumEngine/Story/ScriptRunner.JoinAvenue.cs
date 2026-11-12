using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>The command of plan 22 · U11: <c>joinavenue</c>, Join Avenue's growth, its shops and its visitors (<see cref="JoinAvenue"/>).</summary>
public sealed partial class ScriptRunner
{
    /// <summary>
    /// <c>visit</c> brings the day's visitors on the first coming of a day once the player manages the avenue (RESULT and
    /// <c>{number}</c> how many came, 0 for none); <c>rank</c> is the avenue's rank (1 to 10), <c>visitors</c> every visitor it
    /// has had and <c>next</c> the visitors still wanted for the next rank (each RESULT and <c>{number}</c>); <c>shop
    /// market|flowers|cafe|stones|vitamins|antiques</c> opens that shop with its stock at the rank and waits (RESULT 1), or is
    /// 0 where the shop isn't open yet. Since U11's leftovers: <c>open raffle|dojo|salon|nursery|antiques|stones|vitamins</c>
    /// is 1 once that shop is open at the rank (<c>{number}</c> the rank it opens at); <c>dojo hp|attack|…</c> trains the
    /// Pokémon RESULT names (after <c>choosepokemon</c>) in that stat (RESULT and <c>{number}</c> the effort gained; 0 when it
    /// can gain no more there, 254 for an Egg, 255 with none chosen); <c>salon</c> grooms it (the friendship gained, the same
    /// codes); <c>nursery</c> warms the team's Eggs once a day (RESULT how many, 0 with none or once warmed today);
    /// <c>raffle</c> draws the day's ticket on the game's own chance and gives its prize (RESULT 1, 0 once drawn today;
    /// <c>{item}</c>); <c>visitor 1|2|3</c> has the visitor standing at that place greet the player (the line is the game's,
    /// <c>joinAvenue.visitor.N</c>, said under the visitor's name). Since U11's last pieces: <c>invite 1|2|3</c> is the offer
    /// of the visitor standing there (<see cref="JoinAvenue.Offer"/>: 0 their shop is open, 9 no room at the rank, 1 to 5 the
    /// shop of <see cref="JoinAvenue.Invited"/>; the script opens it with <c>setflag</c>); <c>shop</c> notes the money in hand
    /// and <c>till</c> does so by itself; <c>tally &lt;shop&gt;</c> adds what the money fell by since to the shop's own
    /// (RESULT and <c>{number}</c>); <c>level &lt;shop&gt;</c> is the shop's level (1 to 5) by what was spent in it. The shops'
    /// stock, the dojo's effort, the salon's friendship and the raffle's prizes go by the shop's level.
    /// </summary>
    /// <summary>The money in hand as a Join Avenue shop was come to (<c>shop</c>, <c>till</c>), which <c>tally</c> measures against; never saved.</summary>
    private int joinAvenueTill;

    private void JoinAvenueCommand(Instruction i)
    {
        var story = host.Story;
        int rank = JoinAvenue.Rank(story);
        switch (i.Name)
        {
            case "visit":
                Result = lastNumber = JoinAvenue.Visit(story);
                break;
            case "rank":
                Result = lastNumber = rank;
                break;
            case "visitors":
                Result = lastNumber = story.Var(JoinAvenue.Vars.Visitors);
                break;
            case "next":
                Result = lastNumber = JoinAvenue.ToNextRank(story.Var(JoinAvenue.Vars.Visitors));
                break;
            case "shop":
            {
                string? stock = JoinAvenue.StockOf(story, i.Other);
                Result = stock == null ? 0 : 1;
                if (stock != null)
                {
                    joinAvenueTill = host.Money;
                    host.Open(ScriptScreen.Shop, Subject, stock);
                }
                break;
            }
            case "open":
                lastNumber = JoinAvenue.OpensAt(i.Other);
                Result = JoinAvenue.IsOpen(story, i.Other) ? 1 : 0;
                break;
            case "till":
                joinAvenueTill = host.Money;
                Result = lastNumber = host.Money;
                break;
            case "tally":
                Result = lastNumber = JoinAvenue.Tally(story, i.Other, joinAvenueTill, host.Money);
                joinAvenueTill = 0;
                break;
            case "level":
                Result = lastNumber = JoinAvenue.LevelOf(story, i.Other);
                break;
            case "invite":
                Result = JoinAvenue.Offer(story, int.Parse(i.Other));
                lastNumber = JoinAvenue.RoomAt(rank);
                break;
            case "dojo":
            case "salon":
            {
                var pokemon = Result >= 0 && Result < host.Party.Count ? host.Party.Members[Result] : null;
                if (pokemon == null) { Result = 255; break; }
                if (pokemon.IsEgg) { Result = 254; break; }
                lastWearer = pokemon.DisplayName;
                int level = JoinAvenue.LevelOf(story, i.Name);
                Result = lastNumber = i.Name == "salon" ? JoinAvenue.Groom(pokemon, level) : JoinAvenue.Train(pokemon, StatOf(i.Other), level);
                break;
            }
            case "nursery":
                Result = lastNumber = JoinAvenue.Warm(story, host.Party, rank);
                break;
            case "raffle":
            {
                if (story.Has(JoinAvenue.RaffleTodayFlag)) { Result = 0; break; }
                var item = ItemDatabase.Get(JoinAvenue.RafflePrize(host.Chance.Next(100), JoinAvenue.LevelOf(story, "raffle")));
                if (item == null) { Result = 0; break; }
                story.Set(JoinAvenue.RaffleTodayFlag);
                host.Bag.AddItem(item, 1);
                lastItem = item.Name;
                host.Fanfare(FanfareFor(item));
                host.Say(null, new List<string> { Text.Of("found.received", ("n", 1), ("item", item.DisplayName)), Text.Of("found.putAway", ("n", 1), ("pocket", PocketName(item.Pocket))) });
                Result = 1;
                break;
            }
            case "visitor":
            {
                int n = JoinAvenue.VisitorAt(int.Parse(i.Other), story.Var(JoinAvenue.Vars.Visitors));
                // the visitor's own name stays on what they say after (their invitation)
                speakerShown = Text.Of($"joinAvenue.visitor.{n}.name");
                host.Say(speakerShown, new List<string> { Text.Of($"joinAvenue.visitor.{n}.greeting") });
                Result = n;
                break;
            }
        }
    }

    private static StatType StatOf(string word) => word switch
    {
        "hp" => StatType.HP,
        "attack" => StatType.Attack,
        "defense" => StatType.Defense,
        "speed" => StatType.Speed,
        "spatk" => StatType.SpAttack,
        _ => StatType.SpDefense
    };
}
