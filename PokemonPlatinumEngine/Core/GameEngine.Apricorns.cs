using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Apricorn trees, the Apricorn Box and Kurt's balls in the game (plan 20 · J6). Their rules are GPU-free in
/// <c>Models/Apricorns.cs</c>; this is where the trees on the map are told whether their fruit is on, the Box is
/// opened from the bag or the item button or a script, and Kurt rings when his balls are ready.
/// </summary>
public partial class GameEngine
{
    /// <summary>The item the Box is: a key item, opened like a case.</summary>
    public const string ApricornBoxItem = "Apricorn Box";

    /// <summary>Kurt's id in the phone book.</summary>
    public const string KurtContact = "kurt";

    // What the trees on the map were last told: the Box's revision, the day, and the map they were told on
    private int apricornRevision = -1, apricornDay = -1;
    private Map? apricornMap;
    private bool apricornFromBag;

    /// <summary>Every frame of the field: once the Box, the day or the map has changed, every tree on the map is told whether its fruit is on.</summary>
    private void KeepApricornTreesInView()
    {
        int day = ApricornBox.DayOf(GameClock.Today);
        if (flow.Apricorns.Revision == apricornRevision && apricornDay == day && apricornMap == flow.Map) return;
        apricornRevision = flow.Apricorns.Revision;
        apricornDay = day;
        apricornMap = flow.Map;
        ShowApricornTrees(flow.Map, flow.Apricorns, day);
    }

    /// <summary>Tells every Apricorn tree of a map whether it has its fruit on, on a day.</summary>
    internal static void ShowApricornTrees(Map map, ApricornBox box, int day)
    {
        foreach (var npc in map.NPCs)
            if (npc.IsApricornTree)
                npc.ApricornInFruit = box.InFruit(ApricornBox.TreeKey(npc.ScriptFile ?? "", npc.Key ?? ""), day);
    }

    /// <summary>Opens the Apricorn Box to look into, from the bag, the item button or a script.</summary>
    private void OpenApricornBox(bool fromBag)
    {
        apricornFromBag = fromBag;
        flow.State = GameState.ApricornBox;
        flow.ApricornBoxScreen.Open(flow.Apricorns);
    }

    private void UpdateApricornBox(float dt)
    {
        flow.ApricornBoxScreen.Update(dt);
        if (flow.ApricornBoxScreen.IsActive) return;
        if (apricornFromBag)
        {
            flow.State = GameState.BagMenu;
            flow.BagScreen.Resume();
        }
        else flow.State = GameState.Overworld;
    }

    /// <summary>
    /// A step's chance of Kurt ringing to say his balls are ready (plan 20 · J6): once, the first step after the day
    /// they were ordered has passed, to a player who has the Pokégear and Kurt's number. It rings whatever the ten
    /// minutes of the other calls say, and on the same conditions as they do.
    /// </summary>
    private bool KurtRingsOnAStep()
    {
        if (!HasPokegear || ScriptRunning || flow.State != GameState.Overworld || IsUnderground) return false;
        if (flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY)?.IsCave ?? flow.Map.IsCave) return false;
        if (!flow.Story.Has(Pokegear.ContactFlag(KurtContact)) || PhoneBook.Get(KurtContact) is not { } kurt) return false;
        if (!flow.Apricorns.TakeReadyCall(ApricornBox.DayOf(GameClock.Today))) return false;
        PlaceCall(kurt, incoming: true);
        return true;
    }
}
