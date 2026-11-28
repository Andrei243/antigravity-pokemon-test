using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The camp in the game (plan 10 · F6; docs/mechanics/rulings.md, "The camp"): the start menu's CAMP, offered while the
/// options' FOLLOWER row is on and there is a Pokémon (the team out of their balls is the follower's world, and Platinum's
/// menu stays as it was with the row off), opens <see cref="CampScreen"/>; its item button opens the bag on its berries
/// and the berry chosen is fed to the Pokémon under the cursor, the camp coming back either way.
/// </summary>
public partial class GameEngine
{
    // The bag is open to choose a berry for a Pokémon of the camp
    private bool choosingBerryForCamp;

    /// <summary>
    /// Whether the start menu offers CAMP now: a Pokémon on the team, and the options' FOLLOWER row on or the crafting kit in
    /// the bag (plan 39 · CR6, whose pot is the camp's: so the start menu is as it was until Sinnoh's Hall of Fame). In
    /// Paldea the camp is the region's picnic, offered whatever the rows say (plan 34 · PD7).
    /// </summary>
    internal bool HasCamp => (Settings.Follower || CareOn || HasCraftingKit || Here.Id == RegionDatabase.Paldea) && flow.Party.Count > 0;

    /// <summary>The options' TEAM CARE row in force (plan 35 · BN2, decision 1): on unless turned off under the modern rules.</summary>
    internal bool CareOn => Overworld.FieldHelp.On(Settings.Care, Ruleset.Current.Preset);

    /// <summary>Opens the camp with the team: the visit's cure, if a battle came between, is the screen's first line.</summary>
    internal void OpenCamp()
    {
        if (flow.Party.Count == 0) return;
        flow.CampScreen.Open(flow.Party, flow.CampCare, CampKitchenNow());
        flow.State = GameState.Camp;
        // The team's menu sprites, which the clearing shows
        foreach (var member in flow.Party.Members)
            if (!member.IsEgg) PokemonSprites.Request(member.LookName);
    }

    private void UpdateCamp(float dt)
    {
        flow.CampScreen.Update(dt);
        switch (flow.CampScreen.TakeRequest())
        {
            case CampRequest.Feed:
                choosingBerryForCamp = true;
                flow.State = GameState.BagMenu;
                flow.BagScreen.Registered = flow.RegisteredItem;
                flow.BagScreen.OpenToPick("berries");
                return;
            case CampRequest.Care:
                OpenCare();
                return;
            case CampRequest.Close:
                EmptyThePicnicBasket();
                flow.State = GameState.Overworld;
                return;
        }
        if (!flow.CampScreen.IsActive)
        {
            EmptyThePicnicBasket();
            flow.State = GameState.Overworld;
        }
    }

    /// <summary>
    /// A Paldean picnic packed up (plan 34 · PD7): the Eggs the basket holds for the time the team picnicked go to the team,
    /// or the boxes once it is full, with a line on the toast. Nothing, and no draw, anywhere but Paldea.
    /// </summary>
    private void EmptyThePicnicBasket()
    {
        if (Here.Id != RegionDatabase.Paldea) return;
        var eggs = PicnicBasket.Find(flow.Party, flow.CampScreen.Age, flow.Powers.Strength(PowerKind.Egg),
            SplitMix.Hash((int)(flow.PlayTime * 30), 0xB45EE7u), PlayerIdentity.Number, Ruleset.Current, ShinyCharm.Held(flow.Bag));
        if (eggs.Count == 0) return;
        foreach (var egg in eggs)
        {
            egg.Met(PlaceName(), GameClock.Today);
            if (!flow.Party.Add(egg)) flow.Boxes.Store(egg);
        }
        ShowNotification(Text.Of("picnic.eggs", ("n", eggs.Count)));
    }

    // The bag put away: the berry chosen is eaten by whoever stands under the camp's cursor, and the camp is back
    private void BerryChosenForCamp(ItemData? berry)
    {
        choosingBerryForCamp = false;
        if (berry != null) flow.CampScreen.Feed(berry, flow.Bag);
        else flow.CampScreen.NoBerryChosen();
        flow.State = GameState.Camp;
    }

    // ------------------------------------------------------------------ the care close-up (plan 35 · BN2)

    // The bag is open to choose a berry or a treat for the Pokémon close up
    private bool choosingForCare;

    /// <summary>
    /// Opens the close-up of the Pokémon under the camp's cursor: its model is asked for (never in a headless game, which
    /// renders nothing), and the still sprite stands in until it is built.
    /// </summary>
    internal void OpenCare()
    {
        if (flow.CampScreen.Selected is not { } p) return;
        if (!flow.CareScreen.Open(p, flow.Care, CareDay, GameClock.IsNight)) return;
        if (!IsHeadless)
        {
            PokemonModels.Request(p.LookName);
            PokemonSprites.Request(p.LookName);
        }
        flow.State = GameState.Care;
    }

    private void UpdateCare(float dt)
    {
        flow.CareScreen.Update(dt);
        switch (flow.CareScreen.TakeRequest())
        {
            case CareRequest.Treat:
                choosingForCare = true;
                flow.State = GameState.BagMenu;
                flow.BagScreen.Registered = flow.RegisteredItem;
                flow.BagScreen.OpenToPick("meal");
                return;
            case CareRequest.Close:
                CloseCare();
                return;
        }
        if (!flow.CareScreen.IsActive) CloseCare();
    }

    // Back to the camp, its picture let go
    private void CloseCare()
    {
        if (!IsHeadless) flow.CareScreen.Unload();
        // (opened from the pasture, plan 35 · BN5: back to the meadow, where the script waits)
        flow.State = careFromPasture ? GameState.Overworld : GameState.Camp;
        careFromPasture = false;
    }

    // The bag put away: what was picked is eaten, and the close-up is back
    private void PickedForCare(ItemData? item)
    {
        choosingForCare = false;
        if (item != null) flow.CareScreen.Feed(item, flow.Bag);
        else flow.CareScreen.NothingPicked();
        flow.State = GameState.Care;
    }
}
