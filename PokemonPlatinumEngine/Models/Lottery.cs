using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// Jubilife TV's lottery corner (plan 06 · R14a; <c>src/scrcmd_jubilife_lottery.c</c>, <c>system_vars.c</c> and the
/// script of <c>scripts_jubilife_tv_1f.s</c>): a number of the day, the trainer IDs of the player's Pokémon on the
/// team and in the PC matched against it from the last digit up, and a prize for the most digits matched, once a day.
/// No drawing or input. The number is a story variable (<see cref="NumberVar"/>), as it is one of the original's.
/// </summary>
public static class Lottery
{
    /// <summary>
    /// The variable the day's number is kept in (<c>VAR_LOTTERY_TRAINER_ID_LOW_HALF</c>). The original means to keep
    /// 32 bits in two variables, but writes both halves into this one (<c>SetJubilifeLotteryTrainerID</c>), the high
    /// half last; the script reads this one alone, so the number shown is the high half of what was drawn.
    /// </summary>
    public const string NumberVar = "VAR_LOTTERY_TRAINER_ID_LOW_HALF";

    /// <summary>Set once a ticket has been checked today, and once a prize has been given today (the story's daily flags).</summary>
    public const string CheckedFlag = "FLAG_DAILY_CHECKED_LUCKY_NUMBER", PrizeFlag = "FLAG_DAILY_RECEIVED_LOTTERY_PRIZE";

    /// <summary>How many digits a trainer ID has, which is the most that can match (<c>TRAINER_ID_MAX_LENGTH</c>).</summary>
    public const int Digits = 5;

    /// <summary>The original's general generator (<c>LCRNG_MULTIPLIER</c>) and the lottery's own increment (<c>LOTTERY_LCRNG_INCREMENT</c>).</summary>
    private const uint Multiplier = 1103515245u, Increment = 12345u;

    /// <summary>
    /// A new day's number (<c>SystemVars_SynchronizeJubilifeLotteryTrainerID</c>): the record-mixing group's number
    /// (<see cref="SpecialEncounters.DailyNumber"/>) taken once round the general generator with the lottery's own
    /// increment; what is kept of it is the high half (<see cref="NumberVar"/>).
    /// </summary>
    public static int NumberOf(uint dailyNumber) => (int)(unchecked(dailyNumber * Multiplier + Increment) >> 16);

    /// <summary>
    /// A new game's number (<c>SystemVars_RandomizeJubilifeLotteryTrainerID</c>, which the new game's script runs):
    /// two draws of sixteen bits, of which the second, the high half, is what is kept.
    /// </summary>
    public static int Drawn(Random rng)
    {
        rng.Next(1 << 16);
        return rng.Next(1 << 16);
    }

    /// <summary>
    /// How many digits of a trainer ID match the ticket's, from the last digit up to the first that differs
    /// (<c>CheckTrainerIdForMatch</c>): 0 to 5.
    /// </summary>
    public static int Matching(int ticket, int trainerId)
    {
        ticket &= 0xffff;
        trainerId &= 0xffff;
        int matched = 0;
        for (int i = 0; i < Digits && ticket % 10 == trainerId % 10; i++)
        {
            ticket /= 10;
            trainerId /= 10;
            matched++;
        }
        return matched;
    }

    /// <summary>
    /// The trainer ID the lottery reads off a Pokémon (<c>MON_DATA_OT_ID &amp; 0xffff</c>): its first trainer's, the
    /// card's number of the player for one of their own. Pokémon carry no ID of their own yet (plan 07 · O2): the
    /// trainer they came from (<see cref="Pokemon.OriginalTrainer"/>) stands in for it.
    /// </summary>
    public static int IdOf(Pokemon p, int playerId) => (p.OriginalTrainer?.Id ?? playerId) & 0xffff;

    /// <summary>What a ticket came to: the digits matched, the Pokémon that matched them, and whether it is in the PC.</summary>
    public readonly record struct Match(int Digits, Pokemon? Pokemon, bool InBox);

    /// <summary>
    /// The best match among the player's Pokémon (<c>ScrCmd_CheckForJubilifeLotteryWinner</c>): the first of the team
    /// with the most digits, the first in the PC (box by box) with the most, and the team's when the two are level.
    /// Eggs don't take part (there are none yet).
    /// </summary>
    public static Match Check(int ticket, IEnumerable<Pokemon> team, IEnumerable<Pokemon> boxed, int playerId)
    {
        static (int Digits, Pokemon? Pokemon) Best(IEnumerable<Pokemon> pokemon, int ticket, int playerId)
        {
            (int Digits, Pokemon? Pokemon) best = (0, null);
            foreach (var p in pokemon)
            {
                int digits = Matching(ticket, IdOf(p, playerId));
                if (digits > 0 && digits > best.Digits) best = (digits, p);
            }
            return best;
        }

        var onTeam = Best(team, ticket, playerId);
        var inBox = Best(boxed, ticket, playerId);
        if (onTeam.Digits == 0 && inBox.Digits == 0) return new Match(0, null, false);
        return onTeam.Digits >= inBox.Digits ? new Match(onTeam.Digits, onTeam.Pokemon, false) : new Match(inBox.Digits, inBox.Pokemon, true);
    }

    /// <summary>
    /// The prize for the digits matched (the corner's script): an Ultra Ball for the last digit, a PP Up for two, an
    /// Exp. Share for three, a Max Revive for four, and the Master Ball for all five. Null for none.
    /// </summary>
    public static string? PrizeFor(int digits) => digits switch
    {
        1 => "Ultra Ball",
        2 => "PP Up",
        3 => "Exp. Share",
        4 => "Max Revive",
        5 => "Master Ball",
        _ => null
    };
}
