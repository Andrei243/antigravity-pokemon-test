using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The open world's field beside the grid's (plan 32 · OW1): the game keeps one field at a time, the grid's
/// (<c>flow.Map</c>, <c>flow.Player</c>) or an open map's (<c>flow.Open</c>), and the field's update goes to whichever
/// is in force. Nothing of a region of today reaches the open field: no region's file says <c>open</c> and no open map
/// is loaded by the game, so <c>flow.Open</c> is null in every game and every line here is passed by. A tool or a test
/// lays an open map in (<see cref="AddOpenMap"/>) and puts the player there through the <c>Driver</c>.
/// <para>
/// What OW1 has the open field do is stand and count its ticks: the capsule (OW3), the picture (OW8), doors and
/// scripts (OW5) come later, so the field's frame runs no script, opens no menu and draws what the grid's field drew.
/// </para>
/// </summary>
public partial class GameEngine
{
    private readonly Dictionary<string, OpenMap> openMaps = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The region's word on what its outdoors are walked on, where the player stands (plan 32 · OW1).</summary>
    public FieldKind FieldHere => Here.Field;

    /// <summary>Whether the player is in an open map.</summary>
    public bool InOpenField => flow.Open != null;

    /// <summary>The open map of that name the game knows, or null (every name, today).</summary>
    internal OpenMap? OpenMapNamed(string name) => openMaps.GetValueOrDefault(name);

    /// <summary>
    /// An open map the game is to know by name (plan 32 · OW1: a tool's or a test's; OW2 on, the regions' baked maps).
    /// A name a grid map has is refused, since a save and a warp name a map by its name alone.
    /// </summary>
    internal void AddOpenMap(OpenMap map)
    {
        if (MapDatabase.TryGet(map.Name) != null) throw new ArgumentException($"{map.Name} is a grid map's name; an open map's must be its own");
        openMaps[map.Name] = map;
    }

    /// <summary>Puts the player in an open map at a place, facing a way, with the field's clock begun afresh; the grid's place is kept beside it.</summary>
    internal void EnterOpenField(OpenMap map, OpenPos pos, int yaw)
    {
        if (OpenMapNamed(map.Name) != map) AddOpenMap(map);
        flow.Open = new OpenField(map, pos, yaw);
        flow.State = GameState.Overworld;
    }

    /// <summary>The field's frame in an open map: its ticks counted, and nothing else yet (the player stands until OW3's capsule).</summary>
    private void UpdateOpenField(float dt) => flow.Open!.Advance(dt);

    /// <summary>
    /// A loaded save's open place (plan 32 · OW1): the player is put there if the game knows its map, and otherwise
    /// wakes on the save's grid place, which the game has already put them on.
    /// </summary>
    private void TakeUpOpenPlace(SaveData save)
    {
        flow.Open = null;
        if (save.OpenPlace is not { } saved) return;
        var place = saved.ToPlace();
        if (OpenMapNamed(place.Map) is { } map) flow.Open = new OpenField(map, place.Pos, place.Yaw);
        else Log.Warn($"The save stood in the open map {place.Map}, which isn't there: it wakes on its grid place, {flow.Map.Name} {flow.Player.GridX},{flow.Player.GridY}.");
    }
}
