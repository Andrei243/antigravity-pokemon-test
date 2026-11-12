using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The hub's halls as the story stands (plan 41 · EG2): the Laurel Gallery furnished afresh as the player comes in, the
/// Villa's way (<see cref="FurnishVilla"/>), a plinth for each region finished, each read by its own script.
/// </summary>
public partial class GameEngine
{
    private readonly List<Prop> laurelProps = new();

    /// <summary>Stands the Laurel Gallery's plinths where <see cref="LaurelGallery.Plinths"/> says, and clears the places of the rest.</summary>
    private void FurnishGallery()
    {
        var room = MapDatabase.Get(LaurelGallery.Map);
        if (room.Name != LaurelGallery.Map) return;
        foreach (var prop in laurelProps) room.Props.Remove(prop);
        laurelProps.Clear();
        foreach (var place in LaurelGallery.EveryPlace())
        {
            room.SetSolid(place.X, place.Y, false);
            room.TileScripts.Remove((place.X, place.Y));
        }
        foreach (var plinth in LaurelGallery.Plinths(flow.Story, flow.HallOfFame, flow.Circuit))
        {
            room.SetSolid(plinth.X, plinth.Y, true);
            room.TileScripts[(plinth.X, plinth.Y)] = plinth.Script;
            var prop = new Prop { Type = PropType.Plinth, X = plinth.X, Y = plinth.Y, Model = plinth.Of };
            room.Props.Add(prop);
            laurelProps.Add(prop);
        }
        world?.Forget(room, 0, 0, room.Width, room.Height);
    }
}
