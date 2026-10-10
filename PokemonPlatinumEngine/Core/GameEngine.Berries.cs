using System;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The berry patches in the field (plan 06 · R14a). Their rules are GPU-free in <c>Overworld/BerryPatches.cs</c>; this
/// is where the game counts their minutes (<c>KeepTheEncounterClock</c>), lets them grow once they are seen, and tells
/// each patch of soil on the map what to show.
/// </summary>
public partial class GameEngine
{
    /// <summary>Sinnoh's berry patches (saved as <see cref="SaveData.Berries"/>).</summary>
    private BerryPatches berries = BerryPatches.NewGame();

    /// <summary>How far from the player a patch counts as seen: the original's is "in the camera's view".</summary>
    private const int BerrySight = 8;

    // What the soil on the map was last told: the patches' revision and the map it was told on
    private int berryRevision = -1;
    private Map? berryMap;

    /// <summary>
    /// Every frame of the field: a patch near enough to be seen starts growing (<c>BerryPatches_UpdateGrowthStates</c>),
    /// and once anything has changed every patch of soil on the map is given what it shows now.
    /// </summary>
    private void KeepBerriesInView()
    {
        foreach (var npc in currentMap.NPCs)
            if (npc.BerryPatch is { } seen && Math.Abs(npc.GridX - player.GridX) <= BerrySight && Math.Abs(npc.GridY - player.GridY) <= BerrySight)
                berries.Seen(seen);
        if (berries.Revision == berryRevision && berryMap == currentMap) return;
        berryRevision = berries.Revision;
        berryMap = currentMap;
        ShowBerries(currentMap, berries);
    }

    /// <summary>Gives every patch of soil on a map what its berry patch shows.</summary>
    internal static void ShowBerries(Map map, BerryPatches patches)
    {
        foreach (var npc in map.NPCs)
        {
            if (npc.BerryPatch is not { } id || id < 0 || id >= BerryPatches.Count) continue;
            var patch = patches[id];
            npc.BerryLook = (patch.Stage, patch.Berry, patch.Mulch != Mulch.None);
        }
    }
}
