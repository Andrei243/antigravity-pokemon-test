using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.SuperContests;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Super Contests in the game (plan 06 · R17a): the Contest Hall's receptionists hold one through the scripts'
/// <c>supercontest enter</c>, which fades into <see cref="SuperContestScreen"/> and waits for it; once it is over the
/// field comes back and what it left is counted (the original's <c>Contest_EndContest</c>). The rules are
/// <see cref="SuperContest"/>'s; the Fashion Case is the game's own, saved.
/// </summary>
public partial class GameEngine
{
    /// <summary>The flags a win at the Master Rank sets, one for each quality (the original's <c>SystemFlag_SetContestMaster</c>).</summary>
    public static string ContestMasterFlag(ContestStat category) => "FLAG_CONTEST_MASTER_" + category.ToString().ToUpperInvariant();

    /// <summary>How the last contest came out (<c>supercontest ribbon</c> and <c>prize</c> ask it); nothing is kept of it in a save.</summary>
    private ContestOutcome? contestOutcome;

    /// <summary>
    /// A contest begins for a Pokémon of the team (<c>NewContest</c>, <c>RunContestApplication</c>): the screen opens
    /// behind a fade, and the four Pokémon's menu sprites are asked for as it does.
    /// </summary>
    private void HoldContest(int slot, ContestStat category, ContestRank rank, ContestMode mode)
    {
        contestOutcome = null;
        if (slot < 0 || slot >= flow.Party.Count) return;
        var pokemon = flow.Party.Members[slot];
        bool afterGame = flow.Story.IsRegionComplete("Sinnoh") && flow.Pokedex.NationalUnlocked;
        StartTransition(GameState.SuperContest, () =>
        {
            flow.SuperContestScreen.Begin(pokemon, PlayerIdentity.Name, PlayerIdentity.Look == PlayerLook.Girl, category, rank, mode, afterGame, flow.FashionCase, fieldRandom);
            foreach (var c in flow.SuperContestScreen.Contest!.Contestants) Graphics.PokemonSprites.Request(c.Pokemon.ModelName);
        });
    }

    private void UpdateContest(float dt)
    {
        flow.SuperContestScreen.Update(dt);
        if (flow.SuperContestScreen.IsActive) return;
        FinishContest();
    }

    /// <summary>
    /// The contest has ended (<c>Contest_EndContest</c>): the ribbon, the accessory and the friendship were the
    /// screen's to give as the winner was named; here the opponents' Pokémon go into the Pokédex as seen, the
    /// Trainer Card's score is counted, a win at the Master Rank is remembered, and the field comes back.
    /// </summary>
    private void FinishContest()
    {
        contestOutcome = flow.SuperContestScreen.Outcome;
        if (flow.SuperContestScreen.Contest is { } contest && contestOutcome is { } outcome)
        {
            scriptAnswer = outcome.Placement;
            // The records and the winner's photograph (plan 06 · R17b; RECORD_SUPER_CONTEST_PARTICIPATIONS and its fellows)
            flow.ContestRecords.Count(outcome, contest.Player.Pokemon, flow.SuperContestScreen.Worn, PlayerIdentity.Name, GameClock.Today);
            if (!contest.IsPractice)
            {
                foreach (var rival in contest.Contestants.Where(c => !c.IsPlayer))
                    flow.Pokedex.RegisterSeen(rival.Pokemon.Species.DexNumber);
                if (outcome.Won)
                {
                    flow.TrainerScore = TrainerScore.Add(flow.TrainerScore, TrainerScore.WonContest);
                    if (contest.Rank == ContestRank.Master) flow.Story.Set(ContestMasterFlag(contest.Category));
                }
            }
        }
        // The board stays up until the screen has gone dark
        StartTransition(GameState.Overworld, () =>
        {
            flow.SuperContestScreen.Close();
            PlayFieldMusic();
        });
    }
}
