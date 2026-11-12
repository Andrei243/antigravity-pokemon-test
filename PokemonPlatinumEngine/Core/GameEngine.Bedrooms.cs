using System.Collections.Generic;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The bedroom at home in every region (plan 43 · LF7): its computer opens the shared decorating over the room's floor
/// (<c>openbase bedroom</c>), and what was set down there is laid as props whenever the room is come to, as a secret
/// base's room is (<see cref="LayOutSecretBase"/>). The rules are <see cref="Bedrooms"/>'.
/// </summary>
public partial class GameEngine
{
    // What each bedroom laid out last put in it: its props and the tiles they made solid, taken back before the next laying
    private readonly Dictionary<Map, (List<Prop> Props, HashSet<(int X, int Y)> Solid)> bedroomsLaid = new();

    /// <summary>The region whose bedroom the decorating screen shows; null while it shows none.</summary>
    private string? decoratingBedroom;

    /// <summary>Every bedroom a game laid out is bare again (a new game, a game loaded: the maps outlast it).</summary>
    private void ClearTheBedrooms()
    {
        if (entranceProp != null && entranceMap != null) { entranceMap.Props.Remove(entranceProp); world?.Forget(entranceMap, entranceProp.X - 1, entranceProp.Y - 1, 3, 3); }
        entranceProp = null;
        entranceMap = null;
        foreach (var room in new List<Map>(bedroomsLaid.Keys)) TakeBackTheBedroom(room);
        bedroomsLaid.Clear();
    }

    private void TakeBackTheBedroom(Map room)
    {
        if (!bedroomsLaid.TryGetValue(room, out var laid)) return;
        foreach (var prop in laid.Props) room.Props.Remove(prop);
        foreach (var (x, y) in laid.Solid) room.SetSolid(x, y, false);
        bedroomsLaid.Remove(room);
        world?.Forget(room, 0, 0, room.Width, room.Height);
    }

    /// <summary>A bedroom at home as the player comes in: what stands in it laid as props, in the way where it blocks. Any other room is left alone.</summary>
    internal void LayOutBedroom(Map room)
    {
        KeepTheBaseEntrance();
        if (Bedrooms.RegionOf(room.Name) is not { } region) return;
        TakeBackTheBedroom(room);
        if (!flow.Bedrooms.TryGetValue(region, out var placed) || placed.Count == 0) return;
        var props = new List<Prop>();
        var solid = new HashSet<(int X, int Y)>();
        foreach (var (decoration, x, y) in Bedrooms.Standing(placed, flow.Decorations))
        {
            var (w, d) = decoration.Footprint;
            var prop = new Prop { Type = PropType.Decoration, X = x, Y = y, Width = w, Depth = d, Model = DecorationArt.ModelPrefix + decoration.Id };
            room.Props.Add(prop);
            props.Add(prop);
            foreach (var (tx, ty) in Bedrooms.Blocked(decoration, x, y))
                if (room.InBounds(tx, ty) && !room.IsSolid(tx, ty))
                {
                    room.SetSolid(tx, ty, true);
                    solid.Add((tx, ty));
                }
        }
        bedroomsLaid[room] = (props, solid);
        world?.Forget(room, 0, 0, room.Width, room.Height);
    }

    // The way into the home drawn at its spot on the map it was laid on (plan 43 · LF7)
    private Prop? entranceProp;
    private Map? entranceMap;

    /// <summary>
    /// The way into the player's home at the spot its door stands at, on the map the player is on (plan 43 · LF7;
    /// style guide, "Secret bases", "The way in at a spot"): a card of the spot's kind, laid as the map is come to and
    /// taken off where the home is elsewhere or gone. Nothing changes of the tile, which stays the spot it was.
    /// </summary>
    internal void KeepTheBaseEntrance()
    {
        Prop? want = null;
        if (flow.SecretBase is { } home && home.Map == flow.Map.Name
            && flow.Map.SecretBaseSpots.TryGetValue((home.X, home.Y), out int id) && SecretBaseLayouts.Of(id) is { } spot)
            want = new Prop { Type = PropType.BaseEntrance, X = home.X, Y = home.Y, Width = 1, Depth = 1, Model = spot.Kind.ToString() };
        if (entranceProp != null && entranceMap != null)
        {
            if (want != null && entranceMap == flow.Map && entranceProp.X == want.X && entranceProp.Y == want.Y && entranceProp.Model == want.Model) return;
            entranceMap.Props.Remove(entranceProp);
            world?.Forget(entranceMap, entranceProp.X - 1, entranceProp.Y - 1, 3, 3);
            entranceProp = null;
            entranceMap = null;
        }
        if (want == null) return;
        flow.Map.Props.Add(want);
        entranceProp = want;
        entranceMap = flow.Map;
        world?.Forget(flow.Map, want.X - 1, want.Y - 1, 3, 3);
    }

    /// <summary>The bedroom's computer: the shared decorating over its floor. False where the player stands in no bedroom.</summary>
    private bool OpenBedroomDecoration()
    {
        if (Bedrooms.RegionOf(flow.Map.Name) is not { } region) return false;
        var laid = bedroomsLaid.TryGetValue(flow.Map, out var l) ? l.Solid : null;
        var floor = Bedrooms.FloorOf(flow.Map, region, laid);
        decoratingBedroom = region;
        flow.State = GameState.Decoration;
        var room = Bedrooms.Room(flow.Bedrooms.GetValueOrDefault(region) ?? new List<PlacedDecoration>(), flow.Decorations, floor);
        var session = new DecoratingSession(room, flow.Decorations, SecretBases.KindOf);
        flow.DecorationScreen.Open(session, Text.Of("secretBase.bedroom"), piece => DecorationArt.PlanLook(piece.Kind));
        return true;
    }

    /// <summary>The bedroom's decorating done: what stands in it kept as the region's, laid at once, and the field the player's again.</summary>
    private void FinishBedroomDecoration()
    {
        string region = decoratingBedroom!;
        decoratingBedroom = null;
        Bedrooms.Keep(flow.Bedrooms, region, flow.DecorationScreen.Session.Room);
        Bedrooms.MarkOut(flow.Decorations, flow.SecretBase, flow.Bedrooms);
        LayOutBedroom(flow.Map);
        flow.State = GameState.Overworld;
    }
}
