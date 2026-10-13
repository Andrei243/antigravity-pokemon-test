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

    /// <summary>The Seal Case's Seals and capsules, and the contests' records and photographs (saved).</summary>
    private readonly SealCase sealCase = new();
    private readonly ContestRecords contestRecords = new();
    private readonly SealCaseScreen sealCaseScreen = new();
    private readonly SealShopScreen sealShopScreen = new();
    private readonly FashionCaseScreen fashionScreen = new();

    // A case opened from the bag goes back to the bag when it closes; from a script or the item button, to the field
    private bool capsulesFromBag, fashionFromBag;

    /// <summary>Opens the Ball Capsules (<c>OpenSealCapsuleEditor</c>): from the PC's script, which waits for it, or from the bag.</summary>
    private void OpenBallCapsules(bool fromBag)
    {
        capsulesFromBag = fromBag;
        flow.State = GameState.SealCase;
        sealCaseScreen.Open(sealCase, playerParty);
        foreach (var p in playerParty.Members.Where(p => !p.IsEgg)) Graphics.PokemonSprites.Request(p.ModelName);
    }

    private void UpdateBallCapsules(float dt)
    {
        sealCaseScreen.Update(dt);
        if (sealCaseScreen.IsActive) return;
        // Each capsule changed and kept adds to the Trainer Card's score (TRAINER_SCORE_EVENT_UNK_06)
        for (int i = 0; i < sealCaseScreen.Kept; i++) trainerScore = TrainerScore.Add(trainerScore, TrainerScore.MadeCapsule);
        if (capsulesFromBag)
        {
            flow.State = GameState.BagMenu;
            bagScreen.Resume();
        }
        else flow.State = GameState.Overworld;
    }

    /// <summary>The Seal merchant's stall (<c>PokeMartSeal</c>): the day's seven Seals.</summary>
    private void OpenSealShop()
    {
        flow.State = GameState.SealShop;
        sealShopScreen.Open(SealDatabase.Market(GameClock.Today.DayOfWeek), sealCase);
    }

    private void UpdateSealShop(float dt)
    {
        sealShopScreen.Update(ref playerMoney, ShowNotification, dt);
        if (!sealShopScreen.IsActive) flow.State = GameState.Overworld;
    }

    /// <summary>Opens the Fashion Case to look into, from the bag or the item button.</summary>
    private void OpenFashionCase(bool fromBag)
    {
        fashionFromBag = fromBag;
        flow.State = GameState.FashionCase;
        fashionScreen.Open(fashionCase, contestRecords);
    }

    /// <summary>Shows the photograph in a contest's frame (the Contest Hall's wall); the script waits for it.</summary>
    private void ShowContestPhoto(ContestStat category)
    {
        if (contestRecords.PhotoOf(category) is not { } photo) return;
        fashionFromBag = false;
        Graphics.PokemonSprites.Request(photo.Form ?? photo.Species);
        flow.State = GameState.FashionCase;
        fashionScreen.OpenPhoto(photo);
    }

    private void UpdateFashionCase(float dt)
    {
        fashionScreen.Update(dt);
        if (fashionScreen.IsActive) return;
        if (fashionFromBag)
        {
            flow.State = GameState.BagMenu;
            bagScreen.Resume();
        }
        else flow.State = GameState.Overworld;
    }

    /// <summary>Whether a Master Rank contest has been won, in any of the five: the Trainer Card's star for contests (<c>SystemFlag_CheckContestMaster</c>).</summary>
    private bool IsContestMaster => System.Enum.GetValues<ContestStat>().Any(q => story.Has(ContestMasterFlag(q)));

    /// <summary>
    /// A save from before the Fashion Case was given (plan 06 · R17b) that is past Jubilife City's scene with Team
    /// Galactic, where Jubilife TV's man now gives it, is handed the case and what comes with it as it loads.
    /// </summary>
    private void OweTheFashionCase()
    {
        if (story.Has(FashionCaseFlag) || story.Var("VAR_JUBILIFE_CITY_STATE") < 4) return;
        story.Set(FashionCaseFlag);
        if (ItemDatabase.Get("Fashion Case") is { } item && playerInventory.GetQuantity(item) == 0) playerInventory.AddItem(item, 1);
        fashionCase.FillAsGiven(fieldRandom);
    }
}
