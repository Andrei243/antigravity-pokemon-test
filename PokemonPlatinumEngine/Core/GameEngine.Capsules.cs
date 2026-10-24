using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.SuperContests;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Ball Capsules, Seals and what the contests leave in the game (plan 06 · R17b): the Seal Case and its screen, opened
/// from the PC's menu as the original's is (<c>seal capsules</c>) or from the bag's OPEN; the Seal merchant's stall
/// (<c>seal shop</c>); the Fashion Case looked into from the bag, and a contest winner's photograph from the Contest
/// Hall's wall; and the contests' records. The rules are <see cref="SealCase"/>'s, <see cref="CapsuleRules"/>' and
/// <see cref="ContestRecords"/>'.
/// </summary>
public partial class GameEngine
{
    /// <summary>The flag the Fashion Case is given under (the original's, set by Jubilife TV's man).</summary>
    public const string FashionCaseFlag = "FLAG_RECEIVED_FASHION_CASE";

    // A case opened from the bag goes back to the bag when it closes; from a script or the item button, to the field
    private bool capsulesFromBag, fashionFromBag;

    /// <summary>Opens the Ball Capsules (<c>OpenSealCapsuleEditor</c>): from the PC's script, which waits for it, or from the bag.</summary>
    private void OpenBallCapsules(bool fromBag)
    {
        capsulesFromBag = fromBag;
        flow.State = GameState.SealCase;
        flow.SealCaseScreen.Open(flow.SealCase, flow.Party);
        foreach (var p in flow.Party.Members.Where(p => !p.IsEgg)) Graphics.PokemonSprites.Request(p.LookName);
    }

    private void UpdateBallCapsules(float dt)
    {
        flow.SealCaseScreen.Update(dt);
        if (flow.SealCaseScreen.IsActive) return;
        // Each capsule changed and kept adds to the Trainer Card's score (TRAINER_SCORE_EVENT_UNK_06)
        for (int i = 0; i < flow.SealCaseScreen.Kept; i++) flow.TrainerScore = TrainerScore.Add(flow.TrainerScore, TrainerScore.MadeCapsule);
        if (capsulesFromBag)
        {
            flow.State = GameState.BagMenu;
            flow.BagScreen.Resume();
        }
        else flow.State = GameState.Overworld;
    }

    /// <summary>The Seal merchant's stall (<c>PokeMartSeal</c>): the day's seven Seals.</summary>
    private void OpenSealShop()
    {
        flow.State = GameState.SealShop;
        flow.SealShopScreen.Open(SealDatabase.Market(GameClock.Today.DayOfWeek), flow.SealCase);
    }

    private void UpdateSealShop(float dt)
    {
        flow.SealShopScreen.Update(ref flow.Money, ShowNotification, dt);
        if (!flow.SealShopScreen.IsActive) flow.State = GameState.Overworld;
    }

    /// <summary>Opens the Fashion Case to look into, from the bag or the item button.</summary>
    private void OpenFashionCase(bool fromBag)
    {
        fashionFromBag = fromBag;
        flow.State = GameState.FashionCase;
        flow.FashionCaseScreen.Open(flow.FashionCase, flow.ContestRecords);
    }

    /// <summary>Shows the photograph in a contest's frame (the Contest Hall's wall); the script waits for it.</summary>
    private void ShowContestPhoto(ContestStat category)
    {
        if (flow.ContestRecords.PhotoOf(category) is not { } photo) return;
        fashionFromBag = false;
        Graphics.PokemonSprites.Request(PokemonLook.Of(photo.Form ?? photo.Species, photo.Shiny));
        flow.State = GameState.FashionCase;
        flow.FashionCaseScreen.OpenPhoto(photo);
    }

    private void UpdateFashionCase(float dt)
    {
        flow.FashionCaseScreen.Update(dt);
        if (flow.FashionCaseScreen.IsActive) return;
        if (fashionFromBag)
        {
            flow.State = GameState.BagMenu;
            flow.BagScreen.Resume();
        }
        else flow.State = GameState.Overworld;
    }

    /// <summary>Whether a Master Rank contest has been won, in any of the five: the Trainer Card's star for contests (<c>SystemFlag_CheckContestMaster</c>).</summary>
    private bool IsContestMaster => System.Enum.GetValues<ContestStat>().Any(q => flow.Story.Has(ContestMasterFlag(q)));

    /// <summary>
    /// A save from before the Fashion Case was given (plan 06 · R17b) that is past Jubilife City's scene with Team
    /// Galactic, where Jubilife TV's man now gives it, is handed the case and what comes with it as it loads.
    /// </summary>
    private void OweTheFashionCase()
    {
        if (flow.Story.Has(FashionCaseFlag) || flow.Story.Var("VAR_JUBILIFE_CITY_STATE") < 4) return;
        flow.Story.Set(FashionCaseFlag);
        if (ItemDatabase.Get("Fashion Case") is { } item && flow.Bag.GetQuantity(item) == 0) flow.Bag.AddItem(item, 1);
        flow.FashionCase.FillAsGiven(fieldRandom);
    }
}
