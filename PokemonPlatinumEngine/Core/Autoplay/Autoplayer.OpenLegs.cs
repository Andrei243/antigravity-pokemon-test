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
    // The door of a passage of the grid's the bot is going through to reach another part of the open land; forgotten in
    // a room and with each new leg
    private OpenDoor? openVia;

    /// <summary>
    /// A door into a room of the grid's that leads out by another door to a part of the open land from which
    /// <paramref name="target"/> can be walked to (plan 33 · GL6: the Galar Mine and Galar Mine No. 2, Glimwood Tangle,
    /// between parts of Galar no road joins), the nearest the body can walk to; null for none.
    /// </summary>
    private OpenDoor? PassageToward(OpenMap map, OpenPos feet, OpenPos target, MovementGrants grants)
    {
        OpenDoor? best = null;
        long bestDistance = long.MaxValue;
        foreach (var door in map.Doors)
        {
            if (!door.Open(game.Story.Has) || GridMap(door.Room) is not { } room) continue;
            long d2 = door.Shape.Base.GroundDistanceSquared(feet);
            if (d2 >= bestDistance) continue;
            bool leadsOn = false;
            foreach (var warp in room.Warps)
            {
                if (warp.Shut || engine.OpenMapNamed(warp.TargetMap) != map) continue;
                if (map.DoorBackFrom(room.Name, warp.SourceX, warp.SourceY) is not { } far || far == door || map.Marker(far.Out) is not { } m) continue;
                if (Policy.Route(map, m.Pos, target, grants) != null) { leadsOn = true; break; }
            }
            if (!leadsOn || Policy.Route(map, feet, FootOf(map, door.Shape), grants) == null) continue;
            best = door;
            bestDistance = d2;
        }
        return best;
    }

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
            openVia = null;
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

        // On the way through a passage of the grid's to another part of the land: its door is the place gone for
        if (openVia is { } via)
        {
            target = FootOf(map, via.Shape);
            into = via.Shape;
            who = null;
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
            openPath = Policy.Route(map, feet, target, field.Grants);
            // No way over the open ground: through a passage of the grid's (a mine between two parts of the land), whose
            // far door's marker has a way on to the leg's place; gone for from the next frame
            if (openPath == null && openVia == null && PassageToward(map, feet, target, field.Grants) is { } passage)
            {
                openVia = passage;
                openPath = null;
                return ActionFrame.None;
            }
            openPath ??= new() { feet, target };
            openCorner = 1;
            openTarget = target;
            openSincePath = 0f;
        }
        return Steer(feet, ClearOfOtherDoors(map, PursuitPoint(feet), feet, into), toGo / OpenPos.PerMetre);
    }

    /// <summary>
    /// The point steered for kept out of every door but the leg's own (plan 33 · GL6): the navigation mesh runs along a
    /// wall's foot, where a door's volume stands, and a body steered through one is taken into a room it wasn't going
    /// to (Motostoke's stadium, on the way north out of the town). A point within a metre of another door is put out of
    /// it, on the side the body stands.
    /// </summary>
    private static OpenPos ClearOfOtherDoors(OpenMap map, OpenPos aim, OpenPos feet, OpenShape? mine)
    {
        const int margin = 700;
        foreach (var door in map.Doors)
        {
            var s = door.Shape;
            if (ReferenceEquals(s, mine) || s == mine) continue;
            int x0 = s.Min.X - margin, x1 = s.Max.X + margin, z0 = s.Min.Z - margin, z1 = s.Max.Z + margin;
            if (aim.X < x0 || aim.X > x1 || aim.Z < z0 || aim.Z > z1) continue;
            // Out past the door's outer face (the side its out marker stands on), keeping the way along the wall
            if (map.Marker(door.Out) is not { } outside) continue;
            var o = outside.Pos;
            const int clear = 1_000;
            if (o.Z >= s.Max.Z) return aim with { Z = Math.Max(aim.Z, z1 + clear) };
            if (o.Z <= s.Min.Z) return aim with { Z = Math.Min(aim.Z, z0 - clear) };
            if (o.X >= s.Max.X) return aim with { X = Math.Max(aim.X, x1 + clear) };
            return aim with { X = Math.Min(aim.X, x0 - clear) };
        }
        return aim;
    }

    /// <summary>
    /// Where in an open map a leg goes on to, for choosing a room's way out (plan 33 · GL6: Galar Mine No. 2 opens on
    /// Hulbury and on Motostoke): its trigger, person or marker there, or the door of the open map into its room (or
    /// into a room on the way to it); null where the open map has none of them.
    /// </summary>
    private OpenPos? OpenGoalOf(Leg leg, OpenMap open)
    {
        switch (leg.Kind)
        {
            case LegKind.OpenTrigger: return open.Triggers.FirstOrDefault(t => t.Name == leg.What) is { } t ? FootOf(open, t.Shape) : null;
            case LegKind.OpenTalk: return open.Person(leg.What)?.Pos;
            case LegKind.OpenReach: return open.Marker(leg.What)?.Pos;
        }
        var doors = open.Doors.Where(d => d.Open(game.Story.Has) && (d.Room == leg.Map || Itinerary.NextMap(d.Room, leg.Map, GridMap) != null)).ToList();
        var direct = doors.Where(d => d.Room == leg.Map).ToList();
        return (direct.Count > 0 ? direct : doors).Select(d => (OpenPos?)FootOf(open, d.Shape)).FirstOrDefault();
    }

    /// <summary>Where a volume stands on the ground: its middle at the highest face up between its heights, else its floor.</summary>
    private static OpenPos FootOf(OpenMap map, OpenShape shape)
    {
        var b = shape.Base;
        var footing = map.Ground.Footing(b.X / 1000.0, b.Z / 1000.0, shape.Max.Y / 1000.0, shape.Min.Y / 1000.0);
        return footing is { } f ? new OpenPos(b.X, (int)Math.Round(f.Y * 1000), b.Z) : b;
    }
}
