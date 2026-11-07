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
/// PC) looks into it on the TM maker's tab (<c>RESULT</c> 1, or 0 and nothing opened without the kit). A material is named by its
/// id or its English name; <c>{material}</c> says the last one named in the chosen language and <c>{materials}</c> the
/// number. A host that keeps no pouch does nothing and answers 0.
/// </summary>
public sealed partial class ScriptRunner
{
    private string materialId = "";
    private int materialCount;

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
        if (i.Name == "open")
        {
            if (pouch != null) host.Open(ScriptScreen.Crafting, Subject);
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
