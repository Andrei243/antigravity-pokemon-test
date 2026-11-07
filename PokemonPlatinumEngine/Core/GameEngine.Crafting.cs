using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The crafting kit in the game (plan 39 · CR1; <c>docs/mechanics/rulings.md</c>, "Materials, crafting and cooking"): the
/// kit is a key item looked into from the bag, the item button or a script's <c>craft open</c>; a wild Pokémon felled or
/// caught in the field leaves materials in its pouch while the kit is in the bag, and the field's toast tells what was left
/// once the field is the player's again. Their rules are GPU-free in <c>Models/Materials.cs</c> and
/// <c>Models/MaterialDrops.cs</c>.
/// </summary>
public partial class GameEngine
{
    /// <summary>The item the kit is: a key item of ours, opened like a case.</summary>
    public const string CraftingKitItem = "Crafting Kit";

    /// <summary>The kit's <c>fieldUse</c> in the item table.</summary>
    internal const string CraftingKitUse = "CraftingKit";

    /// <summary>The flag the craftswoman's script sets as she gives the kit (ours): the field's spots (CR2) and the PC's maker (CR4) wait for it.</summary>
    public const string CraftingKitFlag = "FLAG_GOT_CRAFTING_KIT";

    private bool craftingFromBag;

    /// <summary>How many kinds of material the toast names before it says "and N more".</summary>
    internal const int MostKindsToldOf = 4;

    // What a battle left that the field hasn't told yet, until it can
    private List<MaterialCount>? leftBehindToTell;

    /// <summary>Whether the kit is in the bag: nothing of plan 39 is seen or drawn before it, and no battle draws once more.</summary>
    internal bool HasCraftingKit => ItemDatabase.Get(CraftingKitItem) is { } kit && flow.Bag.GetQuantity(kit) > 0;

    /// <summary>Opens the kit to look into, from the bag, the item button or a script.</summary>
    private void OpenCrafting(bool fromBag)
    {
        craftingFromBag = fromBag;
        flow.State = GameState.Crafting;
        flow.CraftingScreen.Open(flow.Crafting);
    }

    private void UpdateCrafting(float dt)
    {
        flow.CraftingScreen.Update(dt);
        if (flow.CraftingScreen.IsActive) return;
        if (craftingFromBag)
        {
            flow.State = GameState.BagMenu;
            flow.BagScreen.Resume();
        }
        else flow.State = GameState.Overworld;
    }

    /// <summary>
    /// What a battle just fought leaves in the pouch (plan 39 · CR1): the wild Pokémon felled or caught in a battle of
    /// the field, each its own, decided on the field's own chance and only while the kit is in the bag, so a game
    /// without it draws nothing more. Never a trainer's Pokémon, nor anything in the Frontier, the Safari Game, Pal
    /// Park's show, the Bug-Catching Contest or the catching lesson; and nothing after a battle lost or run from.
    /// What was left is told once the field is back (<see cref="KeepWhatWasLeft"/>).
    /// </summary>
    private void MaterialsAfterBattle(BattleEngine fought, bool defeat)
    {
        if (defeat || !HasCraftingKit || !LeavesMaterials(fought, InBugContest)) return;
        var foes = fought.FelledFoes.ToList();
        if (fought.Caught is { } caught && !foes.Contains(caught)) foes.Add(caught);
        var left = MaterialDrops.For(foes, fieldRandom);
        var kept = new List<MaterialCount>();
        foreach (var (id, n) in left)
            if (flow.Crafting.Pouch.Add(id, n) is var added and > 0) kept.Add(new MaterialCount(id, added));
        if (kept.Count == 0) return;
        (leftBehindToTell ??= new()).AddRange(kept);
    }

    /// <summary>Whether a battle that has ended is one whose wild Pokémon leave materials: a wild battle of the field, not a trainer's, the Frontier's, the Safari Game's, Pal Park's, the contest's or the lesson.</summary>
    internal static bool LeavesMaterials(BattleEngine fought, bool inBugContest) =>
        !inBugContest && !fought.IsTrainerBattle && !fought.IsFrontier && fought.Kind is BattleKind.Normal or BattleKind.Roamer;

    /// <summary>Tells what the last battle left, once and in one line, while the field is shown with nothing else on it.</summary>
    private void KeepWhatWasLeft()
    {
        if (leftBehindToTell == null || !gameStarted) return;
        if (flow.State != GameState.Overworld || ScriptRunning || flow.Toast.Visible) return;
        var told = leftBehindToTell;
        leftBehindToTell = null;
        ShowNotification(LeftBehindLine(told));
    }

    /// <summary>The toast's line for what was left: "Left behind: Soft Fluff ×2, Ember Core ×1." (one line for all of it).</summary>
    internal static string LeftBehindLine(IEnumerable<MaterialCount> left)
    {
        var merged = new List<MaterialCount>();
        foreach (var m in left)
        {
            int at = merged.FindIndex(x => x.Id == m.Id);
            if (at >= 0) merged[at] = merged[at] with { Count = merged[at].Count + m.Count };
            else merged.Add(m);
        }
        // A horde can leave a dozen kinds: the line shows the first few and counts the rest, to stay one line wide
        string list = string.Join(Text.Of("crafting.leftSeparator"),
            merged.Take(MostKindsToldOf).Select(m => Text.Of("crafting.leftItem", ("name", MaterialTable.NameOf(m.Id)), ("n", m.Count))));
        if (merged.Count > MostKindsToldOf) list = Text.Of("crafting.leftMore", ("list", list), ("n", merged.Count - MostKindsToldOf));
        return Text.Of("crafting.left", ("list", list));
    }

    /// <summary>Forgets what was waiting to be told (a game begun or loaded).</summary>
    private void ForgetWhatWasLeft() => leftBehindToTell = null;
}
