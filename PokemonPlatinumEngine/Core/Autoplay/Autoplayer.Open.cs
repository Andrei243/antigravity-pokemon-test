using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Core.Autoplay;

/// <summary>
/// The playthrough bot in an open map (plan 32 · OW10): it follows an <see cref="OpenItinerary"/> by the navigation
/// mesh's path, steering the stick at each corner in turn as the game reads it (by the world's ways: up is north, as
/// the rules steer, decision 17 being the user's), and plays whatever comes as the grid's bot does: a trainer's walk
/// up and their battle, a trigger's scene, a wild Pokémon met by touch. The stick is fed as the frame's tilt
/// (<see cref="ActionFrame.MoveX"/>, <see cref="ActionFrame.MoveY"/>) with the run button, so a recording of it
/// replays as a player's would.
/// </summary>
internal sealed partial class Autoplayer
{
    /// <summary>The way across an open map the bot follows while the player is in one; with none it stands.</summary>
    public OpenItinerary? OpenWay { get; set; }

    /// <summary>How near the body must come to a corner of the path before it steers for the next, in metres.</summary>
    public const double CornerReach = 0.4;

    // The path being steered along, the corner steered for, what it was laid toward and when, and the progress made
    private List<OpenPos>? openPath;
    private int openCorner;
    private OpenPos openTarget;
    private float openSincePath, openSinceProgress;
    private double openBest = double.MaxValue;
    private OpenLeg? openLeg;
    private int openWildBattles;
    private OpenPos? openLast;

    /// <summary>The wild battles begun in an open map, and how far the body has walked there, in metres.</summary>
    public int OpenWildBattles => openWildBattles;
    public double OpenMetres { get; private set; }

    /// <summary>What the bot steers for in an open map, for a trace: the leg's place, the corner and the path's length.</summary>
    public string OpenSteering => openPath == null ? "no path" : $"to {openTarget}, corner {openCorner} of {openPath.Count}: {openPath[Math.Min(openCorner, openPath.Count - 1)]}";

    private ActionFrame InTheOpenField(float dt)
    {
        engine.Steering = null;
        var field = game.OpenField!;
        var feet = field.Player.Pos;
        if (openLast is { } last) OpenMetres += Math.Sqrt(feet.GroundDistanceSquared(last)) / OpenPos.PerMetre;
        openLast = feet;
        ArrivedOpen(field);
        // A scene, or a trainer walking up: lines read on, nothing steered
        if (engine.ScriptRunning || game.OpenApproach != null) return Pulse(GameAction.Confirm);
        if (OpenWay is not { Finished: false } way)
        {
            Report.Finished = true;
            return ActionFrame.None;
        }
        Report.Finished = false;
        var leg = way.Current!;
        if (!ReferenceEquals(leg, openLeg))
        {
            openLeg = leg;
            openPath = null;
            openBest = double.MaxValue;
            openSinceProgress = 0;
            way.WildAtStart = openWildBattles;
        }
        way.Seconds += dt;
        if (way.Seconds > OpenItinerary.LegSeconds) return GiveUp(way, "out of time");

        // Where the leg is: a marker, a place, or the nearest wild Pokémon of its zone (else the zone's middle)
        OpenPos? where = leg.Kind switch
        {
            OpenLegKind.Marker => field.Map.Marker(leg.What)?.Pos,
            OpenLegKind.Place => leg.At,
            _ => NearestWild(field, leg.What, feet) ?? ZoneMiddle(field.Map, leg.What)
        };
        if (where is not { } target) return GiveUp(way, "nowhere on the map");
        if (leg.Kind == OpenLegKind.MeetWild && openWildBattles > way.WildAtStart)
        {
            Done(way);
            return ActionFrame.None;
        }
        double toGo = Math.Sqrt(feet.GroundDistanceSquared(target));
        if (leg.Kind != OpenLegKind.MeetWild && toGo <= OpenItinerary.Reached && Math.Abs(feet.Y - target.Y) < 2_000)
        {
            Done(way);
            return ActionFrame.None;
        }
        // In the zone's middle with no Pokémon to go up to: it waits there for one to come out (no want of progress)
        if (leg.Kind == OpenLegKind.MeetWild && toGo <= OpenItinerary.Reached && NearestWild(field, leg.What, feet) == null)
        {
            openSinceProgress = 0;
            openBest = double.MaxValue;
            return ActionFrame.None;
        }

        // Progress: a way laid afresh for want of it, the leg given up after its tries
        if (toGo < openBest - 100) { openBest = toGo; openSinceProgress = 0; }
        else openSinceProgress += dt;
        if (openSinceProgress > 3f)
        {
            openSinceProgress = 0;
            openBest = toGo;
            openPath = null;
            if (++way.Tries > OpenItinerary.MostTries) return GiveUp(way, "no progress");
        }

        // The path: laid when there is none, every half second toward a wild Pokémon (it moves), or when the target moved
        openSincePath += dt;
        bool moving = leg.Kind == OpenLegKind.MeetWild;
        if (openPath == null || moving && openSincePath > 0.5f || target.GroundDistanceSquared(openTarget) > 1_000L * 1_000)
        {
            openPath = Policy.Route(field.Map, feet, target, field.Grants);
            openCorner = 1;
            openTarget = target;
            openSincePath = 0;
            if (openPath == null)
            {
                // No way on the mesh (the mesh lacks one, or the target is off it): straight at it, as a script's walk goes
                openPath = new List<OpenPos> { feet, target };
            }
        }
        return Steer(feet, PursuitPoint(feet), toGo / OpenPos.PerMetre, sneak: leg.Kind == OpenLegKind.MeetWild && toGo < 10_000);
    }

    /// <summary>How far ahead along the path the body steers for, in metres: it keeps to the path's line, as the funnel laid it, rather than cutting across to the corner from wherever it has drifted.</summary>
    public const double LookAhead = 0.75;

    /// <summary>
    /// The point steered for: the body's place projected onto the path's leg it is on, then <see cref="LookAhead"/>
    /// along it, or the corner where that is nearer; a corner is passed once the body comes within
    /// <see cref="CornerReach"/> of it or past it along its leg.
    /// </summary>
    private OpenPos PursuitPoint(OpenPos feet)
    {
        var path = openPath!;
        while (true)
        {
            var b = path[Math.Min(openCorner, path.Count - 1)];
            if (openCorner >= path.Count - 1) return b;
            var a = path[openCorner - 1];
            double ax = a.X, az = a.Z, dx = b.X - ax, dz = b.Z - az, length = Math.Sqrt(dx * dx + dz * dz);
            double along = length < 1 ? length : ((feet.X - ax) * dx + (feet.Z - az) * dz) / length;
            if (length < 1 || along >= length || Math.Sqrt(feet.GroundDistanceSquared(b)) / OpenPos.PerMetre < CornerReach)
            {
                openCorner++;
                continue;
            }
            double ahead = Math.Max(0, along) + LookAhead * OpenPos.PerMetre;
            if (ahead >= length) return b;
            return new OpenPos((int)Math.Round(ax + dx / length * ahead), b.Y, (int)Math.Round(az + dz / length * ahead));
        }
    }

    /// <summary>The stick tilted at a corner by the world's ways (x east, y south), a share of it on the last half metre, run where far.</summary>
    private static ActionFrame Steer(OpenPos feet, OpenPos corner, double metresToGo, bool sneak = false)
    {
        double dx = corner.X - feet.X, dz = corner.Z - feet.Z;
        double length = Math.Sqrt(dx * dx + dz * dz);
        if (length < 1) return ActionFrame.None;
        double share = metresToGo < 0.5 ? Math.Max(0.3, metresToGo / 0.5) : 1.0;
        sbyte x = (sbyte)Math.Round(dx / length * 127 * share), y = (sbyte)Math.Round(dz / length * 127 * share);
        var stick = ActionFrame.Stick(x, y);
        // Walked where near, and up to a wild Pokémon (a run carries the player's scent twice as far, OW7)
        if (metresToGo <= 3 || sneak) return stick;
        uint down = stick.Down | ActionFrame.Press(GameAction.Run).Down;
        return new ActionFrame(down, 0) { MoveX = x, MoveY = y };
    }

    private void Done(OpenItinerary way)
    {
        Report.Legs.Add(way.Current!.ToString());
        way.Advance();
        openPath = null;
        openLeg = null;
    }

    private ActionFrame GiveUp(OpenItinerary way, string why)
    {
        Report.LegsGivenUp.Add($"{way.Current} ({why})");
        way.Advance(givenUp: true);
        openPath = null;
        openLeg = null;
        return ActionFrame.None;
    }

    /// <summary>
    /// The nearest wild Pokémon of a zone that will let the player come (not a shy one, which runs from them and can't be
    /// caught up with at a walk), or null: the bot then waits in the zone's middle for one to come out.
    /// </summary>
    private static OpenPos? NearestWild(OpenField field, string zone, OpenPos feet) =>
        field.Wild.All.Where(w => w.Zone.Name == zone && w.Kind.Temperament != OpenTemperament.Shy && w.Mood != OpenWildMood.Fleeing)
            .OrderBy(w => w.Pos.GroundDistanceSquared(feet)).ThenBy(w => w.Id)
            .Select(w => (OpenPos?)w.Pos).FirstOrDefault();

    /// <summary>A zone's middle on its ground (the highest face up between its heights there), or null for a zone the map lacks.</summary>
    internal static OpenPos? ZoneMiddle(OpenMap map, string zone)
    {
        if (map.Zones.FirstOrDefault(z => z.Name == zone) is not { } z) return null;
        long sx = 0, sz = 0;
        foreach (var (x, zz) in z.Polygon) { sx += x; sz += zz; }
        int mx = (int)(sx / z.Polygon.Count), mz = (int)(sz / z.Polygon.Count);
        var footing = map.Ground.Footing(mx / 1000.0, mz / 1000.0, z.MaxY / 1000.0, z.MinY / 1000.0);
        int y = footing is { } f ? (int)Math.Round(f.Y * 1000) : z.MinY;
        return new OpenPos(mx, y, mz);
    }

    /// <summary>The place's area noted as come to (an open map's area key, else the map's name).</summary>
    private void ArrivedOpen(OpenField field)
    {
        Report.Maps.Add(field.Map.Name);
        string place = field.Map.AreaAt(field.Player.Pos)?.Key ?? field.Map.Name;
        if (Report.Arrive(place)) FirstArrival?.Invoke(place);
    }
}
