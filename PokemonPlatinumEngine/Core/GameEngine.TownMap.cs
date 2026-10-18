using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The map of the region in the game (plan 01 · M12): where the rules of <see cref="RegionMap"/> and
/// <see cref="TownMapState"/> meet the game's state. The Town Map out of the bag, Fly's choice and a wall's map are
/// one screen (<see cref="UI.TownMapScreen"/>) opened here in its mode.
/// </summary>
public partial class GameEngine
{
    // The cell of the region's map each room and cave is come to from, worked out once (RegionMap.PlacesOf)
    private Dictionary<string, (int X, int Y)>? regionPlaces;
    private string? regionPlacesOf;

    /// <summary>The map of the region the player is in; null in a region that has none.</summary>
    private RegionMap? RegionMapNow => RegionMap.ForMap(flow.Map.Name);

    /// <summary>
    /// The cell of the region's map the player is in (<c>TownMapContext_Init</c>): their own on the overworld; off
    /// it, the cell they last went in from (the original's exit location) and, where nothing remembers that, the
    /// cell the place they are in is come to from. Null where nothing says.
    /// </summary>
    private (int X, int Y)? RegionCellNow(RegionMap region)
    {
        if (flow.Map.Name == region.Map) return RegionMap.CellOf(flow.Player.GridX, flow.Player.GridY);
        if (flow.ExitSpot is { } exit && exit.Map == region.Map) return RegionMap.CellOf(exit.X, exit.Y);
        if (regionPlaces == null || regionPlacesOf != region.Region)
        {
            regionPlaces = region.PlacesOf(MapDatabase.MapNames.Select(MapDatabase.Get));
            regionPlacesOf = region.Region;
        }
        return regionPlaces.TryGetValue(flow.Map.Name, out var cell) ? cell : null;
    }

    /// <summary>What the game has put on the region's map now.</summary>
    private TownMapState? TownMapNow()
    {
        if (RegionMapNow is not { } region) return null;
        var overworld = MapDatabase.Get(region.Map);
        return new TownMapState(region, flow.Story, RegionCellNow(region), flow.MapTrail, town => overworld.Areas.Any(a => a.Key == town.Area && a.Open));
    }

    /// <summary>
    /// Opens the map of the region to be looked at: the Town Map out of the bag (it goes back to the bag) or off
    /// the item button, or a map on a wall. False where the region has no map.
    /// </summary>
    private bool OpenTownMap(TownMapMode mode, bool fromBag, int flier = -1)
    {
        if (TownMapNow() is not { } state) return false;
        townMapFromBag = fromBag;
        flow.TownMapScreen.Open(state, mode, flier);
        flow.State = GameState.FlyMap;
        return true;
    }

    /// <summary>The map is put away: back to the team Fly was chosen from, the bag the Town Map came out of, or the field.</summary>
    private void CloseTownMap()
    {
        if (flow.TownMapScreen.Mode == TownMapMode.Fly)
        {
            flow.State = GameState.PartyMenu;
            flow.PartyScreen.IsActive = true;
        }
        else if (townMapFromPokegear)
        {
            // The map card goes back to the Pokégear, on its own card (plan 20 · J3)
            townMapFromPokegear = false;
            OpenPokegear(PokegearCard.Map);
        }
        else if (townMapFromBag)
        {
            flow.State = GameState.BagMenu;
            flow.BagScreen.Resume();
        }
        else flow.State = GameState.Overworld;
    }

    /// <summary>
    /// A first arrival that the region's map waits to hear of (plan 01 · M12): the original's own map scripts set a
    /// landmark's flag as the player first comes there, and where no script of ours does, the map's file names the
    /// place whose arrival does (<see cref="RegionMap.ArrivalFlags"/>).
    /// </summary>
    private void NoteArrivalOnTheMap()
    {
        if (RegionMapNow is not { } region) return;
        foreach (string flag in region.ArrivalFlags(flow.Map.Name)) flow.Story.Set(flag);
        if (flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY)?.Key is { Length: > 0 } key)
            foreach (string flag in region.ArrivalFlags(key)) flow.Story.Set(flag);
    }

    /// <summary>
    /// Coming onto the overworld by a door, a ship or a flight is written into the way come with no way faced
    /// (<c>OverworldMapHistory_PushViaWarp</c>, from <c>FieldSystem_InitFlagsOnMapChange</c>).
    /// </summary>
    private void NoteTheTrailOnArrival()
    {
        if (RegionMapNow is not { } region || region.Map != flow.Map.Name) return;
        var (x, y) = RegionMap.CellOf(flow.Player.GridX, flow.Player.GridY);
        flow.MapTrail.PushViaWarp(x, y);
    }

    /// <summary>The region's picture shows the hidden places the story has revealed, and is painted again when another is.</summary>
    private void KeepTheRegionsPicture()
    {
        // (A game with no window has no picture to paint, plan 16 · T12)
        if (IsHeadless) return;
        foreach (var region in RegionDatabase.All)
            if (RegionMap.Of(region.Id) is { } map)
                RegionMapPictures.Show(map, map.Hidden.Where(h => flow.Story.Var(h.Variable) == h.Value).Select(h => h.Key));
    }
}
