using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Soaring over a region (plan 38 · EX7, part a; the rulings' "Soaring"; style guide, "Soaring"): from Fly's choice,
/// once the region's Hall of Fame is set, the team's flier carries the player over the region's picture. A flight is
/// a place in the region map's cells (fractions of a cell, the Town Map's own grid), a heading, a height of one to
/// five and a speed; the arrows or the stick turn, climb and dive, Run is faster, a beacon stands over every place
/// Fly lands that is open, Confirm over one lands there and Cancel lands at the nearest open one. Every step is a
/// function of the input it is handed and the time; nothing is drawn from any chance. No drawing or input.
/// </summary>
public sealed class Soaring
{
    /// <summary>Speeds in cells a second, cruising and with Run held; turning in degrees a second; climbing in heights a second.</summary>
    public const float Cruise = 2.5f, Fast = 5f, TurnRate = 110f, ClimbRate = 1.5f;

    /// <summary>The heights: 1 just over the land, 5 the highest; a flight begins at 3.</summary>
    public const float Lowest = 1f, Highest = 5f, Start = 3f;

    /// <summary>How near a beacon's foot (a cell's middle) the flier must be for Confirm to land there.</summary>
    public const float BeaconReach = 0.75f;

    public TownMapState Map { get; }

    /// <summary>Where the flier is, in cells (a cell's middle is its number plus a half).</summary>
    public float X { get; private set; }
    public float Y { get; private set; }

    /// <summary>The way it flies, in degrees from north, clockwise.</summary>
    public float Heading { get; private set; }

    public float Height { get; private set; } = Start;

    /// <summary>The bounds it flies within: the region map's view, in cells.</summary>
    public (float Left, float Top, float Right, float Bottom) Bounds { get; }

    /// <summary>A flight from a cell (the player's, or the map's start where none is known), facing north.</summary>
    public Soaring(TownMapState map, (int X, int Y)? from = null)
    {
        Map = map;
        var view = map.Region.View;
        Bounds = (view.X, view.Y, view.X + view.Width, view.Y + view.Height);
        var at = from ?? map.Here ?? map.Region.Start;
        X = Math.Clamp(at.X + 0.5f, Bounds.Left, Bounds.Right);
        Y = Math.Clamp(at.Y + 0.5f, Bounds.Top, Bounds.Bottom);
    }

    /// <summary>
    /// Why a flight can't begin, as a key of <c>ui.json</c>, or null: the region's Hall of Fame not yet set, no map of the
    /// region, or nobody to carry the player (the flier is the one whose Fly it is, or the helper the field lends).
    /// </summary>
    public static string? WhyNot(bool hallOfFame, RegionMap? map, bool flier)
    {
        if (map == null) return "soar.refused.noMap";
        if (!hallOfFame) return "soar.refused.hallOfFame";
        if (!flier) return "soar.refused.noFlier";
        return null;
    }

    /// <summary>
    /// One step of the flight: <paramref name="turn"/> from -1 (left) to 1 (right), <paramref name="climb"/> from -1
    /// (dive) to 1 (climb), and whether Run is held. The flier stops at the bounds' edge and slides along it.
    /// </summary>
    public void Step(float turn, float climb, bool fast, float dt)
    {
        if (dt <= 0f) return;
        Heading = ((Heading + Math.Clamp(turn, -1f, 1f) * TurnRate * dt) % 360f + 360f) % 360f;
        Height = Math.Clamp(Height + Math.Clamp(climb, -1f, 1f) * ClimbRate * dt, Lowest, Highest);
        float speed = (fast ? Fast : Cruise) * dt, rad = Heading * MathF.PI / 180f;
        X = Math.Clamp(X + MathF.Sin(rad) * speed, Bounds.Left, Bounds.Right);
        Y = Math.Clamp(Y - MathF.Cos(rad) * speed, Bounds.Top, Bounds.Bottom);
    }

    /// <summary>Puts the flier at a place and heading (a tool's and a test's; never a step of the flight).</summary>
    public void Place(float x, float y, float heading = 0f)
    {
        X = Math.Clamp(x, Bounds.Left, Bounds.Right);
        Y = Math.Clamp(y, Bounds.Top, Bounds.Bottom);
        Heading = (heading % 360f + 360f) % 360f;
    }

    /// <summary>The places with a beacon: every one of Fly's places that is open.</summary>
    public IEnumerable<TownMapSpot> Beacons => Map.Spots.Where(s => s.Open);

    private float Distance(TownMapSpot spot) =>
        spot.Spot.Cells.Min(c => MathF.Sqrt(MathF.Pow(c.X + 0.5f - X, 2) + MathF.Pow(c.Y + 0.5f - Y, 2)));

    /// <summary>The beacon under the flier, within <see cref="BeaconReach"/> of its foot, or null (a shut place has none).</summary>
    public TownMapSpot? BeaconUnder =>
        Beacons.Select(s => (Spot: s, Far: Distance(s))).Where(s => s.Far <= BeaconReach).OrderBy(s => s.Far).Select(s => s.Spot).FirstOrDefault();

    /// <summary>Confirm: the place landed at, the beacon under the flier, or null when there is none.</summary>
    public TownMapSpot? Land() => BeaconUnder;

    /// <summary>Cancel: the open place nearest the flier, where the flight ends; null only where nothing is open.</summary>
    public TownMapSpot? Nearest() => Beacons.OrderBy(Distance).ThenBy(s => s.Town.Id).FirstOrDefault();
}
