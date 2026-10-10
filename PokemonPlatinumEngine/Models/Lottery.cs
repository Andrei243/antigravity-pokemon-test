using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The Jubilife TV Lottery Corner (plan 06 · R14a; the original's <c>scrcmd_jubilife_lottery.c</c> and
/// <c>SystemVars_SynchronizeJubilifeLotteryTrainerID</c>): a ticket number a day, read from the day's number, matched
/// digit by digit from the right against the ID numbers of the original trainers of the player's Pokémon, on the team
/// and in the boxes. The more digits match, the better the prize. No drawing or input.
/// </summary>
public static class Lottery
{
    /// <summary>How many digits a ticket and an ID number have.</summary>
    public const int Digits = 5;

    /// <summary>
    /// The day's ticket. The original works out the day's number × 1,103,515,245 + 12,345 and means to keep both halves,
    /// but writes the high half over the low one (<c>SetJubilifeLotteryTrainerID</c> sets
    /// <c>VAR_LOTTERY_TRAINER_ID_LOW_HALF</c> twice), and the ticket shown is that variable: the high half.
    /// </summary>
    public static int TicketOf(uint dailyNumber) => (int)(unchecked(dailyNumber * 1103515245u + 12345u) >> 16);

    /// <summary>How many of the two numbers' last digits are the same, up to five (<c>CheckTrainerIdForMatch</c>).</summary>
    public static int Matching(int ticket, int id)
    {
        int matched = 0;
        for (int i = 0; i < Digits && ticket % 10 == id % 10; i++)
        {
            ticket /= 10;
            id /= 10;
            matched++;
        }
        return matched;
    }

    /// <summary>The best match on the team and in the boxes, and whose it is.</summary>
    public readonly record struct Draw(int Digits, Pokemon? Winner, bool InBox);

    /// <summary>
    /// <c>ScrCmd_CheckForJubilifeLotteryWinner</c>: the first Pokémon with the most matching digits on the team, the
    /// first in the boxes, and the team's wins a tie. A Pokémon whose original trainer is the player
    /// (<see cref="Pokemon.OriginalTrainer"/> null) carries <paramref name="playerId"/>; only the card's 16 bits count.
    /// </summary>
    public static Draw Check(int ticket, int playerId, IEnumerable<Pokemon> team, IEnumerable<Pokemon> boxed)
    {
        var (teamDigits, teamWinner) = Best(ticket, playerId, team);
        var (boxDigits, boxWinner) = Best(ticket, playerId, boxed);
        if (teamDigits == 0 && boxDigits == 0) return new Draw(0, null, false);
        return teamDigits >= boxDigits ? new Draw(teamDigits, teamWinner, false) : new Draw(boxDigits, boxWinner, true);
    }

    private static (int Digits, Pokemon? Winner) Best(int ticket, int playerId, IEnumerable<Pokemon> pokemon)
    {
        int best = 0;
        Pokemon? winner = null;
        foreach (var p in pokemon)
        {
            // An Egg's trainer isn't read (MON_DATA_IS_EGG)
            if (p.IsEgg) continue;
            int id = (p.OriginalTrainer?.Id ?? playerId) & 0xffff;
            int matched = Matching(ticket, id);
            if (matched > best)
            {
                best = matched;
                winner = p;
            }
        }
        return (best, winner);
    }

    /// <summary>The prize for so many digits (<c>JubilifeTV1F_SetLotteryPrize</c>); null for none.</summary>
    public static string? Prize(int digits) => digits switch
    {
        1 => "Ultra Ball",
        2 => "PP Up",
        3 => "Exp. Share",
        4 => "Max Revive",
        5 => "Master Ball",
        _ => null
    };
}
