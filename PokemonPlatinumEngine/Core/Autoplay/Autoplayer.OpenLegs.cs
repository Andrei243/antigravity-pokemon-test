using System;
using System.Linq;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Core.Autoplay;

/// <summary>
/// The bot on an <see cref="Autoplay.Itinerary"/> while it stands in an open map (plan 33 · GL6, the first itinerary that
/// walks an open region and its grid rooms as one): a leg of the open map (<see cref="LegKind.OpenTrigger"/>,
/// <see cref="LegKind.OpenTalk"/>, <see cref="LegKind.OpenReach"/>) is steered for along the navigation mesh as an
/// <see cref="OpenItinerary"/>'s leg is, and a leg of a grid room is gone for through the open map's door into that room
/// (or into a room whose ways lead to it). The grid's side goes the other way: a leg of the open map from a room leaves
/// by the room's way out (<see cref="Elsewhere"/>).
/// </summary>
internal sealed partial class Autoplayer
{
    /// <summary>How long a leg may take in an open map, in seconds of field time: the open region's roads are long.</summary>
    public const float OpenLegSeconds = 600f;

    /// <summary>How near someone of an open map the body comes before the confirm button is pressed at them, in millimetres.</summary>
    public const int OpenSpeakAt = 1200;

    private int openFollowIndex = -1;
    private float openInsideSeconds;

    private ActionFrame FollowOpen(Itinerary it, float dt)
    {
        var field = game.OpenField!;
        var map = field.Map;
        var feet = field.Player.Pos;
        var story = game.Story;
        var leg = it.Current!;
        Report.Finished = false;
        if (openFollowIndex != it.Index)
        {
            openFollowIndex = it.Index;
            legClock = 0f;
            openPath = null;
            openBest = double.MaxValue;
            openSinceProgress = 0f;
            openInsideSeconds = 0f;
        }

        // Someone spoken to: over once their script has been seen through and what the leg was for holds
        if (it.Taken)
        {
            it.Taken = false;
            if (it.SawScript && (leg.Until == null || leg.Until(story))) { Next(it); return ActionFrame.None; }
            if (Failed(it, it.SawScript ? "what it was for didn't come" : "nothing started")) return ActionFrame.None;
            openPath = null;
        }

        legClock += dt;
        if (legClock > OpenLegSeconds)
        {
            legClock = 0f;
            openPath = null;
            if (Failed(it, "out of time")) return ActionFrame.None;
        }
        if (leg.IsOpen && leg.Map != map.Name) return Failed(it, $"the leg's open map {leg.Map} isn't this one") ? ActionFrame.None : ActionFrame.None;

        OpenPos target;
        OpenShape? into = null;
        OpenPerson? who = null;
        switch (leg.Kind)
        {
            case LegKind.OpenReach:
            {
                if (leg.Until?.Invoke(story) == true) { Next(it); return ActionFrame.None; }
                if (map.Marker(leg.What) is not { } marker) return Failed(it, "no such marker") ? ActionFrame.None : ActionFrame.None;
                target = marker.Pos;
                if (leg.Until == null && Math.Sqrt(feet.GroundDistanceSquared(target)) <= OpenItinerary.Reached && Math.Abs(feet.Y - target.Y) < 2_000)
                {
                    Next(it);
                    return ActionFrame.None;
                }
                break;
            }
            case LegKind.OpenTrigger:
            {
                if (map.Triggers.FirstOrDefault(t => t.Name == leg.What) is not { } trigger) return Failed(it, "no such trigger") ? ActionFrame.None : ActionFrame.None;
                bool done = trigger.Variable != null ? !trigger.Waits(story.Var) && (leg.Until == null || leg.Until(story)) : leg.Until?.Invoke(story) == true;
                if (done) { Next(it); return ActionFrame.None; }
                target = FootOf(map, trigger.Shape);
                into = trigger.Shape;
                break;
            }
            case LegKind.OpenTalk:
            {
                if (map.Person(leg.What) is not { } person) return Failed(it, "nobody of that name here") ? ActionFrame.None : ActionFrame.None;
                if (leg.Until?.Invoke(story) == true) { Next(it); return ActionFrame.None; }
                if (!person.Present) return Failed(it, "not on the map now") ? ActionFrame.None : ActionFrame.None;
                who = person;
                target = person.Pos;
                break;
            }
            default:
            {
                // A leg of a grid room: through the open map's door into it, or into a room on the way to it
                var doors = map.Doors.Where(d => d.Open(story.Has) && (d.Room == leg.Map || Itinerary.NextMap(d.Room, leg.Map, GridMap) != null)).ToList();
                var direct = doors.Where(d => d.Room == leg.Map).ToList();
                if (direct.Count > 0) doors = direct;
                if (doors.Count == 0) return Failed(it, $"no door of {map.Name} leads to {leg.Map}") ? ActionFrame.None : ActionFrame.None;
                var door = doors.OrderBy(d => d.Shape.Base.GroundDistanceSquared(feet)).First();
                target = FootOf(map, door.Shape);
                into = door.Shape;
                break;
            }
        }

        double toGo = Math.Sqrt(feet.GroundDistanceSquared(target));
        if (who != null && toGo <= OpenSpeakAt)
        {
            // Turned to them by the last steps, then the button: their script runs, or the leg is tried again
            it.Taken = true;
            it.SawScript = false;
            openPath = null;
            return ActionFrame.Press(GameAction.Confirm);
        }

        // Standing in a trigger or a door's volume with nothing started: out of it and back in
        var centre = new OpenPos(feet.X, feet.Y + 800, feet.Z);
        if (into != null && into.Contains(centre))
        {
            openInsideSeconds += dt;
            if (openInsideSeconds > 1f)
            {
                var b = into.Base;
                double dx = feet.X - b.X, dz = feet.Z - b.Z, length = Math.Max(1, Math.Sqrt(dx * dx + dz * dz));
                int reach = Math.Max(into.Max.X - into.Min.X, into.Max.Z - into.Min.Z) + 3_000;
                var outside = new OpenPos((int)(b.X + dx / length * reach), feet.Y, (int)(b.Z + dz / length * reach));
                if (openInsideSeconds > 4f) openInsideSeconds = 0f;
                return Steer(feet, outside, 5);
            }
        }
        else openInsideSeconds = 0f;

        // Progress: a way laid afresh for want of it, the leg tried again after its tries
        if (toGo < openBest - 100) { openBest = toGo; openSinceProgress = 0f; }
        else openSinceProgress += dt;
        if (openSinceProgress > 4f)
        {
            openSinceProgress = 0f;
            openBest = toGo;
            openPath = null;
            if (++reroutes > 8)
            {
                reroutes = 0;
                if (Failed(it, $"no progress at {feet}")) return ActionFrame.None;
            }
        }

        // The last metres into a volume are walked straight at it: the mesh ends at a wall's foot, and a door is in the wall
        if (into != null && toGo < 2_500) return Steer(feet, target, toGo / OpenPos.PerMetre);

        openSincePath += dt;
        bool moving = who != null;
        if (openPath == null || moving && openSincePath > 0.5f || target.GroundDistanceSquared(openTarget) > 1_000L * 1_000)
        {
            openPath = Policy.Route(map, feet, target, field.Grants) ?? new() { feet, target };
            openCorner = 1;
            openTarget = target;
            openSincePath = 0f;
        }
        return Steer(feet, PursuitPoint(feet), toGo / OpenPos.PerMetre);
    }

    /// <summary>Where a volume stands on the ground: its middle at the highest face up between its heights, else its floor.</summary>
    private static OpenPos FootOf(OpenMap map, OpenShape shape)
    {
        var b = shape.Base;
        var footing = map.Ground.Footing(b.X / 1000.0, b.Z / 1000.0, shape.Max.Y / 1000.0, shape.Min.Y / 1000.0);
        return footing is { } f ? new OpenPos(b.X, (int)Math.Round(f.Y * 1000), b.Z) : b;
    }
}
