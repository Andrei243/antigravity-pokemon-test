using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Contest;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The commands of plan 21 · H11: <c>appeals</c>, Hoenn's contests (<see cref="AppealRound"/>), and <c>pokeblock</c>, the
/// Berry Blender and the Pokéblock Case (<see cref="Pokeblocks"/>), on the lines of Emerald's scripts they stand for
/// (<c>data/scripts/contest_hall.inc</c>, <c>LilycoveCity_ContestLobby</c>, <c>data/scripts/berry_blender.inc</c>).
/// </summary>
public sealed partial class ScriptRunner
{
    private AppealRank appealRank = AppealRank.Normal;
    private ContestStat appealCategory = ContestStat.Cool;
    private string lastWinner = "", lastWinnerMon = "";

    /// <summary>
    /// <c>rank</c> and <c>category</c> set what is entered; <c>check</c> asks whether the Pokémon chosen (the slot in
    /// RESULT, as <c>choosepokemon</c> leaves it) may enter (<c>GetContestEntryEligibility</c>: 0 it may, 1 none was chosen,
    /// 2 an Egg or fainted, 3 it lacks the ribbon of the rank before, 4 it may, having won this rank already);
    /// <c>enter</c> holds the contest for the Pokémon checked, the script waiting for it, and RESULT is the player's place,
    /// 0 for the winner (a win at the Master Rank sets <c>FLAG_HOENN_CONTEST_MASTER_&lt;QUALITY&gt;</c>); <c>ribbon</c> asks
    /// what that win gave (RESULT 1 with a ribbon the Pokémon hadn't, <c>{ribbon}</c>), <c>prize</c> whether a Luxury Ball
    /// is due (a Master Rank winner of a rank it had won; RESULT 1; the script gives it), <c>painter</c> whether the
    /// painter comes (a Master Rank win of 800 points or more); <c>hang</c> takes the painting to the museum (RESULT 1
    /// when the Artist Ribbon was given with it, <c>{ribbon}</c>); <c>records</c> counts the contests entered or won;
    /// <c>winner</c> asks the Contest Hall's wall for the winner at a place (1 to 6, the newest first): RESULT 1 when one
    /// hangs there, <c>{wall}</c> and <c>{wallmon}</c> name the trainer and the Pokémon. <c>{contestant}</c> names the
    /// Pokémon entered.
    /// </summary>
    private void Appeals(Instruction i)
    {
        switch (i.Name)
        {
            case "rank":
                appealRank = Enum.Parse<AppealRank>(i.Other, ignoreCase: true);
                break;
            case "category":
                appealCategory = Enum.Parse<ContestStat>(i.Other, ignoreCase: true);
                break;
            case "check":
            {
                int slot = Result;
                contestSlot = -1;
                if (slot < 0 || slot >= host.Party.Count)
                {
                    Result = 1;
                    break;
                }
                Result = HoennContestRules.Check(host.Party.Members[slot], appealCategory, appealRank) switch
                {
                    AppealEntry.Equal => 0,
                    AppealEntry.Above => 4,
                    AppealEntry.TooLow => 3,
                    _ => 2
                };
                if (Result is 0 or 4) contestSlot = slot;
                break;
            }
            case "enter":
            {
                if (contestSlot < 0 || contestSlot >= host.Party.Count) throw Wrong(i, "no Pokémon has been checked for the contest (appeals check)");
                var category = appealCategory;
                var rank = appealRank;
                host.HoldAppeals(contestSlot, category, rank);
                afterBusy = () =>
                {
                    Result = host.AppealOutcome?.Placement ?? 0;
                    if (host.AppealOutcome is { Won: true, Rank: AppealRank.Master }) host.Story.Set(HoennContestRules.MasterFlag(category));
                };
                break;
            }
            case "ribbon":
                Result = host.AppealOutcome?.Ribbon != null ? 1 : 0;
                if (host.AppealOutcome?.Ribbon is { } ribbon) lastRibbon = RibbonDatabase.Name(ribbon);
                break;
            case "prize":
                Result = host.AppealOutcome is { GaveLuxuryBall: true } ? 1 : 0;
                break;
            case "painter":
                Result = host.AppealOutcome is { PaintingOffered: true } ? 1 : 0;
                break;
            case "hang":
            {
                Result = 0;
                if (host.AppealOutcome is not { PaintingOffered: true } outcome || contestSlot < 0 || contestSlot >= host.Party.Count) break;
                var pokemon = host.Party.Members[contestSlot];
                host.HoennContests.HangInMuseum(HoennContestRules.PaintingOf(outcome, pokemon, host.PlayerName));
                host.Story.Set(HoennContestRules.PaintingFlag(outcome.Category));
                if (HoennContestRules.GivesArtistRibbon(pokemon) && RibbonRules.Give(pokemon, HoennContestRules.ArtistRibbon))
                {
                    lastRibbon = RibbonDatabase.Name(HoennContestRules.ArtistRibbon);
                    Result = 1;
                }
                break;
            }
            case "records":
                Result = i.Other == "won" ? host.HoennContests.TotalWon : host.HoennContests.TotalEntered;
                break;
            case "winner":
            {
                int place = int.Parse(i.Other) - 1;
                var hall = host.HoennContests.Hall;
                Result = place < hall.Count ? 1 : 0;
                if (place < hall.Count)
                {
                    lastWinner = hall[place].Trainer;
                    lastWinnerMon = hall[place].Nickname.Length > 0 ? hall[place].Nickname : LocalNames.Species(hall[place].Species);
                }
                break;
            }
        }
    }

    /// <summary>
    /// <c>blend</c> with so many others at the blender: the bag chooses the player's berry, the blender plays and the
    /// script waits (RESULT 1 when a Pokéblock went into the case, 0 when the player backed out or the case was full);
    /// <c>count</c> is how many Pokéblocks the case holds and <c>full</c> is 1 when it can take no more.
    /// </summary>
    private void PokeblockOp(Instruction i)
    {
        switch (i.Name)
        {
            case "blend":
                host.BlendPokeblocks(i.Number);
                afterBusy = () => Result = host.PokeblockKept ? 1 : 0;
                break;
            case "count":
                Result = host.PokeblockCase.Count;
                break;
            case "full":
                Result = host.PokeblockCase.IsFull ? 1 : 0;
                break;
        }
    }
}
