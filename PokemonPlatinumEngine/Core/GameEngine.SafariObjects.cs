using System;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Johto Safari Zone's objects in the game (plan 38 · EX8; <see cref="SafariObjects"/> has the rules): the Area
/// Customizer's screen over an area (the shared decorating screen, <c>safari objects</c>), what it leaves kept, and the
/// objects standing in the zone as props of ours wherever it is come to.
/// </summary>
public partial class GameEngine
{
    /// <summary>The area being arranged at the Area Customizer, by its number in <see cref="SafariObjects.AreaKeys"/>; -1 for none.</summary>
    private int arrangingSafari = -1;

    /// <summary>The area of the Area Customizer's order opened to arrange (the script's <c>safari objects</c>); false where it can't be.</summary>
    private bool ArrangeSafari(int area)
    {
        if (area < 0 || area >= SafariObjects.AreaKeys.Length) return false;
        var map = MapDatabase.Get(SafariObjects.ZoneMap);
        string key = SafariObjects.AreaKeys[area];
        if (map.Name != SafariObjects.ZoneMap || map.AreaBounds(key) is not { } bounds) return false;
        var before = flow.SafariObjects.In(key);
        var store = SafariObjects.StoreOf(before);
        var room = SafariObjects.Room(map, bounds, before, store);
        arrangingSafari = area;
        flow.State = GameState.Decoration;
        var session = new DecoratingSession(room, store, SafariObjects.KindOf);
        int number = area + 1;
        flow.DecorationScreen.Open(session, Text.Of("safariObjects.title", ("area", Text.Of($"safariObjects.area.{number}"))), piece => SafariArt.PlanLook(piece.Kind));
        return true;
    }

    /// <summary>The arranging done: what stands in the area is kept, laid out in the zone at once, and the field is the player's again.</summary>
    private void FinishSafariArrangement()
    {
        string key = SafariObjects.AreaKeys[arrangingSafari];
        arrangingSafari = -1;
        var today = ApricornBox.DayOf(GameClock.Today);
        flow.SafariObjects.Set(key, SafariObjects.Keep(flow.DecorationScreen.Session.Room, flow.SafariObjects.In(key), today));
        LayOutSafariObjects();
        flow.State = GameState.Overworld;
    }

    /// <summary>Every object standing in the zone set down as a prop of ours on its tiles (none of which it blocks), the areas built again.</summary>
    private void LayOutSafariObjects()
    {
        var map = MapDatabase.Get(SafariObjects.ZoneMap);
        if (map.Name != SafariObjects.ZoneMap) return;
        lock (map.Props) map.Props.RemoveAll(p => p.Model.StartsWith(SafariArt.ModelPrefix, StringComparison.Ordinal));
        foreach (string key in SafariObjects.AreaKeys)
            foreach (var o in flow.SafariObjects.In(key))
                if (SafariObjects.TypeOf(o.Type) is { } type)
                    lock (map.Props) map.Props.Add(new Prop { Type = PropType.SafariObject, X = o.X, Y = o.Y, Width = type.Width, Depth = type.Depth, Model = SafariArt.ModelPrefix + type.Key });
        foreach (string key in SafariObjects.AreaKeys)
            if (map.AreaBounds(key) is { } b) world?.Forget(map, b.X, b.Y, b.Width, b.Height);
    }

    private sealed partial class FieldHost
    {
        public bool ArrangeSafari(int area) => game.ArrangeSafari(area);
    }

    internal sealed partial class Driver
    {
        /// <summary>The objects standing in the Johto Safari Zone (plan 38 · EX8).</summary>
        public SafariObjects SafariObjects => game.flow.SafariObjects;
        /// <summary>An area opened at the Area Customizer, as <c>safari objects</c> opens it.</summary>
        public bool ArrangeSafari(int area) => game.ArrangeSafari(area);
        /// <summary>The objects laid out in the zone as an arrival lays them.</summary>
        public void LayOutSafariObjects() => game.LayOutSafariObjects();
    }
}
