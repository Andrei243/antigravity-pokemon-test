using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 39 · CR1: <c>craft</c>, the crafting kit and its pouch of materials. <c>has</c> asks whether the
/// kit is in the bag (<c>RESULT</c> 1 or 0); <c>give kit</c> puts the kit in the bag once and sets
/// <c>FLAG_GOT_CRAFTING_KIT</c> (<c>RESULT</c> 1, or 0 when it was held already); <c>give "Soft Fluff" 3</c> adds to the
/// pouch (<c>RESULT</c> how many fitted, 999 of a kind at most), <c>take</c> removes (<c>RESULT</c> 1, or 0 and nothing
/// taken with fewer), <c>count</c> says how many are held, <c>open</c> looks into the kit, and <c>tm</c> (plan 39 · CR4, from a
/// PC) looks into it on the TM maker's tab (<c>RESULT</c> 1, or 0 and nothing opened without the kit). Plan 39 · CR3's
/// apprentice asks <c>tradable</c> (<c>RESULT</c> 1 with three of some type's common material held, <c>{material}</c> naming
/// it) and <c>trade</c> (three of it for one of its type's rare, <c>{material}</c> naming the rare). CR6's <c>pantry</c> opens
/// the kit on the pantry's goods for sale, and <c>meal "&lt;dish&gt;" [grade]</c> serves a dish to the team there and then
/// (<c>RESULT</c> 1 when it started its power). A material is named by its
/// id or its English name; <c>{material}</c> says the last one named in the chosen language and <c>{materials}</c> the
/// number. A host that keeps no pouch does nothing and answers 0.
/// </summary>
public sealed partial class ScriptRunner
{
    private string materialId = "";
    private int materialCount;

    /// <summary>Whether the crafting kit is in the bag: nothing of plan 39 happens without it.</summary>
    private bool HasCraftingKit() => ItemDatabase.Get(GameEngine.CraftingKitItem) is { } kit && host.Bag.GetQuantity(kit) > 0;

    private void CraftCommand(Instruction i)
    {
        var kit = ItemDatabase.Get(GameEngine.CraftingKitItem);
        bool held = kit != null && host.Bag.GetQuantity(kit) > 0;
        if (i.Name == "has")
        {
            Result = held ? 1 : 0;
            return;
        }
        if (i.Name == "give" && i.Other == "kit")
        {
            Result = 0;
            if (kit == null || held) return;
            host.Bag.AddItem(kit, 1);
            host.Story.Set(GameEngine.CraftingKitFlag);
            lastItem = kit.Name;
            Result = 1;
            return;
        }
        var pouch = host.Crafting?.Pouch;
        // The TM maker of a PC (plan 39 · CR4): the kit's screen on its TMs tab; nothing without the kit in the bag
        if (i.Name == "tm")
        {
            Result = held && pouch != null ? 1 : 0;
            if (Result == 1) host.Open(ScriptScreen.Crafting, Subject, "tm");
            return;
        }
        // A gathering spot of the field (plan 39 · CR2): RESULT 1 with something taken today, `{material}` and
        // `{materials}` naming it, or 0 when it has been picked today. Nothing, and no draw, without the kit
        if (i.Name == "gather")
        {
            Result = 0;
            if (!held || pouch == null || Subject?.GatherKind is not { } kind) return;
            string spot = Gathering.KeyOf(Subject.ScriptFile ?? place, Subject.Key ?? Subject.Name);
            if (Gathering.Pick(host.Crafting!, spot, kind, ApricornBox.DayOf(GameClock.Today), host.Chance) is not { } found) return;
            (materialId, materialCount) = (found.Id, found.Count);
            Result = 1;
            return;
        }
        // A tree cut or a rock smashed with the kit in the bag (plan 39 · CR2): one time in three, a piece of its
        // material (RESULT 1, `{material}` naming it); without the kit nothing and no draw
        if (i.Name == "cleared")
        {
            Result = 0;
            if (!held || pouch == null) return;
            if (Gathering.Cleared(pouch, i.Other == "tree", host.Chance) is not { } left) return;
            (materialId, materialCount) = (left.Id, left.Count);
            Result = 1;
            return;
        }
        if (i.Name == "open")
        {
            if (pouch != null) host.Open(ScriptScreen.Crafting, Subject);
            return;
        }
        // The workshop's pantry counter (plan 39 · CR6): the kit's screen on its PANTRY tab, which sells into the pouch
        if (i.Name == "pantry")
        {
            Result = held && pouch != null ? 1 : 0;
            if (Result == 1) host.Open(ScriptScreen.Crafting, Subject, "pantry");
            return;
        }
        // A dish eaten there and then (plan 39 · CR6; CR7's waiters): the team heals and is cured by the grade, and the
        // dish's power starts where the host keeps powers. A type's encounters is of the Normal type, there being no berry
        if (i.Name == "meal")
        {
            var dish = Meals.Dish(i.Other)!;
            var served = Meals.Serve(host.Party, dish, (DishGrade)i.Number, host.Powers, visit: null, PokemonType.Normal);
            Result = served.Power != null ? 1 : 0;
            return;
        }
        // The workshop's apprentice (plan 39 · CR3): three of the most-held common material for one of its type's rare
        if (i.Name == "tradable")
        {
            var common = pouch == null ? null : Crafting.Tradable(pouch);
            Result = common != null ? 1 : 0;
            if (common != null) (materialId, materialCount) = (common.Id, Crafting.TradeCommons);
            return;
        }
        if (i.Name == "trade")
        {
            var rare = pouch == null ? null : Crafting.Trade(pouch);
            Result = rare != null ? 1 : 0;
            if (rare != null) (materialId, materialCount) = (rare.Id, 1);
            return;
        }
        if (i.Other.Length > 0) materialId = i.Other;
        if (pouch == null)
        {
            Result = 0;
            return;
        }
        switch (i.Name)
        {
            case "give":
                materialCount = Result = pouch.Add(i.Other, i.Number);
                break;
            case "take":
                Result = pouch.Take(i.Other, i.Number) ? 1 : 0;
                materialCount = Result == 1 ? i.Number : 0;
                break;
            case "count":
                materialCount = Result = pouch.Count(i.Other);
                break;
        }
    }
}
