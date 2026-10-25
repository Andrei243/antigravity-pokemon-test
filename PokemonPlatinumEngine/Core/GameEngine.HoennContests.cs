using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Contest;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Hoenn's contests and Pokéblocks in the game (plan 21 · H11): the Contest Lobby's receptionist holds a contest through
/// the scripts' <c>appeals enter</c>, which fades into <see cref="AppealScreen"/> and waits for it; once it is over the
/// field comes back and what it left is counted (<see cref="HoennContestRules.Record"/>). The Pokéblock Case opens from
/// the bag or the item button (<see cref="PokeblockCaseScreen"/>), and the Berry Blender is a berry chosen from the bag
/// and <see cref="BlenderScreen"/>. The rules are <see cref="AppealRound"/>'s, <see cref="HoennContestRules"/>' and
/// <see cref="Pokeblocks"/>'.
/// </summary>
public partial class GameEngine
{
    // ------------------------------------------------------------------ the appeals

    /// <summary>
    /// A contest begins for a Pokémon of the team (<c>ContestHall_EventScript_DoContest</c>): the screen opens behind a
    /// fade, and the four Pokémon's menu sprites are asked for as it does.
    /// </summary>
    private void HoldAppeals(int slot, ContestStat category, AppealRank rank)
    {
        flow.AppealOutcome = null;
        if (slot < 0 || slot >= flow.Party.Count) return;
        var pokemon = flow.Party.Members[slot];
        bool afterGame = flow.Story.IsRegionComplete(RegionDatabase.Hoenn);
        StartTransition(GameState.Appeals, () =>
        {
            flow.AppealScreen.Begin(pokemon, PlayerIdentity.Name, category, rank, afterGame, fieldRandom);
            foreach (var a in flow.AppealScreen.Round!.Appealers) Graphics.PokemonSprites.Request(a.Pokemon.ModelName);
        });
    }

    private void UpdateAppeals(float dt)
    {
        flow.AppealScreen.Update(dt);
        if (flow.AppealScreen.IsActive) return;
        FinishAppeals();
    }

    /// <summary>
    /// The contest has ended (<c>Contest_EndContest</c>): the ribbon was the screen's to give as the winner was named; here
    /// the contests entered and won are counted, the winner hangs on the Contest Hall's wall, the opponents' Pokémon go
    /// into the Pokédex as seen, a win at the Master Rank is remembered and the field comes back.
    /// </summary>
    private void FinishAppeals()
    {
        var screen = flow.AppealScreen;
        flow.AppealOutcome = screen.Outcome;
        if (screen.Round is { } round && screen.Outcome is { } outcome && !screen.Practice)
        {
            flow.ScriptAnswer = outcome.Placement;
            HoennContestRules.Record(outcome, round.Appealers[screen.Player].Pokemon, PlayerIdentity.Name, flow.HoennContests);
            foreach (var rival in round.Appealers.Where(a => !a.IsPlayer)) flow.Pokedex.RegisterSeen(rival.Pokemon);
            if (outcome.Won) flow.TrainerScore = TrainerScore.Add(flow.TrainerScore, TrainerScore.WonContest);
        }
        StartTransition(GameState.Overworld, () =>
        {
            flow.AppealScreen.Close();
            PlayFieldMusic();
        });
    }

    // ------------------------------------------------------------------ the Pokéblock Case

    private bool pokeblockCaseFromBag;

    /// <summary>Opens the Pokéblock Case, from the bag (which it goes back to) or from the item button (back to the field).</summary>
    private void OpenPokeblockCase(bool fromBag)
    {
        pokeblockCaseFromBag = fromBag;
        flow.State = GameState.PokeblockCase;
        flow.PokeblockCaseScreen.Open(flow.PokeblockCase, flow.Party);
    }

    private void UpdatePokeblockCase(float dt)
    {
        flow.PokeblockCaseScreen.Update(dt);
        if (flow.PokeblockCaseScreen.IsActive) return;
        if (pokeblockCaseFromBag)
        {
            flow.State = GameState.BagMenu;
            flow.BagScreen.Resume();
        }
        else flow.State = GameState.Overworld;
    }

    // ------------------------------------------------------------------ the Berry Blender

    private bool choosingBerryToBlend;
    private int blendOthers;

    /// <summary>The berries the others at the blender bring: the first of the berries, Cheri to Pomeg.</summary>
    public const int BlenderGuestBerries = 43;

    /// <summary>The blending begins (<c>pokeblock blend</c>): the bag on its berries, to choose the player's; the script waits.</summary>
    private void StartBlending(int others)
    {
        blendOthers = others;
        flow.PokeblockKept = false;
        choosingBerryToBlend = true;
        flow.ScriptItem = null;
        flow.State = GameState.BagMenu;
        flow.BagScreen.Registered = flow.RegisteredItem;
        flow.BagScreen.OpenToPick("berries");
    }

    private void BerryChosenToBlend(ItemData? berry)
    {
        choosingBerryToBlend = false;
        if (berry?.Berry == null || !flow.Bag.RemoveItem(berry, 1))
        {
            flow.State = GameState.Overworld;
            return;
        }
        BeginBlender(berry, blendOthers, fieldRandom);
    }

    /// <summary>
    /// The blender with the player's berry and so many others' (the guests' berries drawn from the first of the berries,
    /// never two alike, so that no block comes out black by the guests' doing).
    /// </summary>
    internal void BeginBlender(ItemData berry, int others, Random chance)
    {
        var pool = ItemDatabase.GetAll().Where(i => i.Berry != null).OrderBy(i => i.Id).Take(BlenderGuestBerries).Where(i => i.Name != berry.Name).ToList();
        var berries = new List<BlenderBerry> { BlenderBerry.Of(berry) };
        var names = new List<string> { PlayerIdentity.Name };
        for (int g = 0; g < Math.Clamp(others, 0, 3); g++)
        {
            var pick = pool[chance.Next(pool.Count)];
            pool.Remove(pick);
            berries.Add(BlenderBerry.Of(pick));
            int nth = g + 1;
            names.Add(Text.Of($"blender.guest.{nth}"));
        }
        flow.BlenderScreen.Begin(berries, names, flow.PokeblockCase, chance);
        flow.State = GameState.Blender;
    }

    private void UpdateBlender(float dt)
    {
        flow.BlenderScreen.Update(dt);
        if (flow.BlenderScreen.IsActive) return;
        flow.PokeblockKept = flow.BlenderScreen.Kept;
        flow.State = GameState.Overworld;
    }

    private sealed partial class FieldHost
    {
        public HoennContestRecords HoennContests => game.flow.HoennContests;
        public PokeblockCase PokeblockCase => game.flow.PokeblockCase;
        public AppealOutcome? AppealOutcome => game.flow.AppealOutcome;
        public bool PokeblockKept => game.flow.PokeblockKept;

        public void HoldAppeals(int slot, ContestStat category, AppealRank rank) => game.HoldAppeals(slot, category, rank);

        public void BlendPokeblocks(int others) => game.StartBlending(others);
    }
}
