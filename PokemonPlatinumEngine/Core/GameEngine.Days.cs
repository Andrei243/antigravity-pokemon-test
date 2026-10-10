using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The day's events, berries and the lottery (plan 06 · R14a). The rules are GPU-free in <c>Models/</c>
/// (<see cref="DailyEvents"/>, <see cref="BerryPatches"/>, <see cref="Lottery"/>); this is where the game hands them its
/// state: the patches in view as the player comes into a place, the soil ahead for the bag, a berry, a mulch or the
/// Sprayduck used from the bag, the bag opened for a script to choose from, and what the scripts' host reads.
/// </summary>
public partial class GameEngine
{
    /// <summary>What grows in the soft soil (saved as <see cref="SaveData.Berries"/>).</summary>
    private BerryPatches berries = BerryPatches.NewGame();

    // While a script has the bag open to choose from (chooseitem): what it may choose among
    private Inventory? choosingFrom;

    /// <summary>The bag as it shows: the player's, or what a script lets them choose among.</summary>
    private Inventory BagShows => choosingFrom ?? playerInventory;

    /// <summary>
    /// The patches in view begin to grow (<c>BerryPatches_UpdateGrowthStates</c>, which the original runs as the player
    /// comes into another place): what a new game found in fruit waits until then.
    /// </summary>
    private void SeeBerryPatches()
    {
        foreach (var npc in currentMap.NPCs)
            if (npc is { IsBerryPatch: true, Patch: { } patch } && BerryPatches.InView(player.GridX, player.GridY, npc.GridX, npc.GridY))
                berries.See(patch);
    }

    /// <summary>The soft soil the player faces, at the height they stand at; null when it isn't soil.</summary>
    private NPC? PatchAhead()
    {
        var (dx, dy) = FieldMovement.Delta(player.Facing);
        int x = player.GridX + dx, y = player.GridY + dy;
        var npc = currentMap.NpcIn(x, y, currentMap.SurfaceAt(x, y, player.HeightOn(currentMap)).Height);
        return npc is { IsBerryPatch: true, Patch: not null } ? npc : null;
    }

    /// <summary>Whether the player faces soft soil with nothing growing in it, where USE plants a berry (<c>BERRY_PATCH_FLAG_EMPTY</c>).</summary>
    private bool SoilAhead() => PatchAhead() is { Patch: { } patch } && berries.IsEmpty(patch);

    /// <summary>
    /// A berry, a mulch or the Sprayduck used from the bag or the item button (<c>UseBerryFromMenu</c>,
    /// <c>UseMulchFromMenu</c>, <c>UseSprayDuckFromMenu</c>): on the soil faced, by the common scripts the original
    /// runs (its berry tree script's entries 1 to 3). The Sprayduck can't be used while someone travels with the
    /// player, as in the original. False when the item is none of these.
    /// </summary>
    private bool UseOnSoil(ItemData item)
    {
        bool berry = BerryPatches.CanPlant(item), mulch = BerryPatches.MulchOf(item) != Mulch.None, duck = item.FieldUse == "Sprayduck";
        if (!berry && !mulch && !duck) return false;
        if (duck && partner != null)
        {
            ShowNotification(FieldMoveRules.Why(FieldMoveError.Partner));
            AudioManager.PlaySound("error");
            return true;
        }
        var soil = PatchAhead();
        int patch = soil?.Patch ?? -1;
        // CanUseMulch and CanUseSprayDuck: mulch only on empty soil with none on it yet, water only where something grows
        bool fits = soil != null && (berry ? berries.IsEmpty(patch) : mulch ? berries.CanMulch(patch) : berries.HasBerry(patch));
        if (!fits)
        {
            ShowNotification("It can't be used here.");
            AudioManager.PlaySound("error");
            return true;
        }
        StartScript(berry ? FieldScripts.PlantBerry : mulch ? FieldScripts.UseMulch : FieldScripts.UseSprayduck, soil, item: (item.Name, 1));
        return true;
    }

    /// <summary>
    /// What a script may choose among when it opens the bag (<c>OpenBerriesBag</c>, <c>OpenItemsBag</c>): the berries
    /// that can be planted, or the mulches, as many as the player has.
    /// </summary>
    private Inventory ChoosableOf(string kind)
    {
        var shown = new Inventory();
        foreach (var stack in playerInventory.AllItems)
            if (kind == "mulch" ? BerryPatches.MulchOf(stack.Data) != Mulch.None : BerryPatches.CanPlant(stack.Data))
                shown.AddItem(stack.Data, stack.Quantity);
        return shown;
    }

    /// <summary>The bag opened for a script to choose a berry or a mulch.</summary>
    private void OpenBagToChoose(string kind)
    {
        choosingFrom = ChoosableOf(kind);
        currentState = GameState.BagMenu;
        bagScreen.Registered = registeredItem;
        bagScreen.PlayerName = PlayerIdentity.Name;
        bagScreen.SoilAhead = false;
        bagScreen.OpenToChoose(kind == "mulch" ? ItemPocket.Items : ItemPocket.Berries, kind == "mulch" ? "LAY WHICH MULCH?" : "PLANT WHICH BERRY?");
    }

    /// <summary>The bag has closed on a script's choice: its answer is the item's number, nought for none.</summary>
    private void ChoiceFromBag()
    {
        if (!bagScreen.Choosing) return;
        scriptAnswer = bagScreen.Chosen?.Id ?? 0;
        choosingFrom = null;
    }

    private sealed partial class FieldHost
    {
        public DateTime Today => GameClock.Today;
        public BerryPatches Berries => game.berries;
        public IEnumerable<Pokemon> Stored => game.pcBoxStorage.All;
        public void AddScore(int points) => game.trainerScore = TrainerScore.Add(game.trainerScore, points);
    }
}
