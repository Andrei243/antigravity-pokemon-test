using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core.Autoplay;

/// <summary>
/// The bot on an <see cref="Autoplay.Itinerary"/> (plan 16 · T13): while legs are left, the field's free frames go to
/// the current leg instead of the bot's own goals. The way to a leg's tile is searched with no bound but a count of
/// tiles (<see cref="Policy.Route"/>), across maps by their warps (<see cref="Itinerary.NextMap"/>), followed a step
/// at a time and searched again whenever the player is off it or stands still against something; armed triggers
/// that are not the leg's are kept off where another way exists. Scenes, battles and questions on the way are played
/// as the bot plays them anywhere.
/// </summary>
internal sealed partial class Autoplayer
{
    /// <summary>How long a leg may take in the free field before it counts as failed (a leg of training excepted).</summary>
    public const float LegSeconds = 150f;

    /// <summary>The way through the story the bot is following; null for its own goals.</summary>
    public Itinerary? Itinerary { get; set; }

    /// <summary>The lead's share of its HP under which a trainer goes to the nurse.</summary>
    public const float TrainHealShare = 0.45f;

    private List<Direction>? route;
    private List<(int X, int Y)>? routeTiles;
    private string routeMap = "";
    private int routeLeg = -1;
    private float stillSeconds;
    private (string Map, int X, int Y) lastTile;
    private int reroutes;
    private float legClock;
    private bool trainToSecond;
    private bool healing;
    private int healingLevel = -1;
    private readonly Dictionary<(string, string), string?> nextMaps = new();
    private bool detoured;

    /// <summary>The team as it stands, a line each: the species and the level.</summary>
    public IEnumerable<string> Team => game.Party.Members.Select(p => $"{p.Species.Name} {p.Level} ({p.CurrentHP}/{p.MaxHP} HP)");

    private ActionFrame Follow(Itinerary it, float dt)
    {
        var map = game.Map;
        var player = game.Player;
        var leg = it.Current!;
        if (routeLeg != it.Index)
        {
            routeLeg = it.Index;
            legClock = 0f;
            Forget();
            healing = false;
        }

        // A leg whose thing was done: over once what it started is over, or tried again if nothing started
        if (it.Taken)
        {
            it.Taken = false;
            if (leg.Kind == LegKind.Train) Forget();
            else if ((it.SawScript || leg.Kind is LegKind.Enter or LegKind.Reach) && (leg.Until == null || leg.Until(game.Story))) { Next(it); return ActionFrame.None; }
            else if (Failed(it, it.SawScript ? "what it was for didn't come" : "nothing started")) return ActionFrame.None;
        }

        legClock += dt;
        if (leg.Kind != LegKind.Train && legClock > LegSeconds)
        {
            legClock = 0f;
            Forget();
            if (Failed(it, "out of time")) return ActionFrame.None;
        }

        switch (leg.Kind)
        {
            case LegKind.Enter:
                if (map.Name == leg.Map) { Next(it); return ActionFrame.None; }
                return Elsewhere(it, leg.Map);
            case LegKind.Reach:
                if (map.Name == leg.Map && player.GridX == leg.X && player.GridY == leg.Y) { Next(it); return ActionFrame.None; }
                return Toward(it, leg.Map, (x, y) => x == leg.X && y == leg.Y, leg.X, leg.Y, null);
            case LegKind.Trigger:
                return ToTrigger(it, leg);
            case LegKind.Talk:
            {
                if (map.Name != leg.Map) return Elsewhere(it, leg.Map);
                var who = map.NPCs.FirstOrDefault(n => n.Key == leg.What && (leg.Place == null || (n.ScriptFile ?? map.ScriptFileAt(n.GridX, n.GridY)) == leg.Place));
                if (who == null) return Failed(it, "nobody of that key here") ? ActionFrame.None : ActionFrame.None;
                return ToSpeak(it, who);
            }
            case LegKind.Heal:
                return ToNurse(it, leg.Map);
            case LegKind.Train:
                return Train(it, leg);
        }
        return ActionFrame.None;
    }

    private void Next(Itinerary it, bool givenUp = false)
    {
        it.Advance(givenUp);
        (givenUp ? Report.LegsGivenUp : Report.Legs).Add(givenUp ? (it.GivenUp[^1]) : it.Done[^1]);
        Forget();
        engine.Steering = null;
    }

    // A try of the leg failed: true when the leg is given up for it
    private bool Failed(Itinerary it, string why)
    {
        Forget();
        engine.Steering = null;
        if (++it.Tries < Itinerary.MostTries) return false;
        Report.LegsGivenUp.Add($"{it.Current} ({why})");
        it.Advance(givenUp: true);
        return true;
    }

    private void Forget()
    {
        route = null;
        routeTiles = null;
        reroutes = 0;
        stillSeconds = 0f;
    }

    private ActionFrame ToTrigger(Itinerary it, Leg leg)
    {
        var map = game.Map;
        if (map.Name != leg.Map) return Elsewhere(it, leg.Map);
        var trigger = map.Triggers.FirstOrDefault(t => t.Script == leg.What && (leg.Place == null || t.ScriptFile == leg.Place));
        if (trigger == null)
            return Failed(it, "no such trigger") ? ActionFrame.None : ActionFrame.None;
        if (!Armed(trigger))
        {
            // Already played, by the way the story went: nothing to step onto
            Next(it);
            return ActionFrame.None;
        }
        var player = game.Player;
        if (trigger.Covers(player.GridX, player.GridY))
        {
            // Standing on it with nothing started: off and on again
            return Toward(it, leg.Map, (x, y) => !trigger.Covers(x, y), player.GridX, player.GridY, null);
        }
        return Toward(it, leg.Map, trigger.Covers, trigger.X + trigger.Width / 2, trigger.Y + trigger.Depth / 2, trigger, onLastStep: () => it.Taken = true);
    }

    private bool Armed(StepTrigger t) => t.Variable == null || game.Story.Var(t.Variable) == t.Value;

    // To a tile beside someone (or across a counter from them), then turned to them and spoken to
    private ActionFrame ToSpeak(Itinerary it, NPC who)
    {
        var map = game.Map;
        var player = game.Player;
        var stands = StandsFor(map, who);
        if (stands.TryGetValue((player.GridX, player.GridY), out var face))
        {
            Forget();
            if (player.Facing != face)
            {
                engine.Steering = (face, false);
                return ActionFrame.None;
            }
            engine.Steering = null;
            it.Taken = true;
            it.SawScript = false;
            game.Interact();
            return ActionFrame.None;
        }
        return Toward(it, map.Name, (x, y) => stands.ContainsKey((x, y)), who.GridX, who.GridY, null);
    }

    private static Dictionary<(int X, int Y), Direction> StandsFor(Map map, NPC who)
    {
        var stands = new Dictionary<(int X, int Y), Direction>();
        foreach (var way in new[] { Direction.Down, Direction.Up, Direction.Left, Direction.Right })
        {
            var (dx, dy) = FieldMovement.Delta(way);
            stands.TryAdd((who.GridX - dx, who.GridY - dy), way);
            if (map.IsCounter(who.GridX - dx, who.GridY - dy)) stands.TryAdd((who.GridX - 2 * dx, who.GridY - 2 * dy), way);
        }
        return stands;
    }

    private ActionFrame ToNurse(Itinerary it, string center)
    {
        var map = game.Map;
        if (map.Name != center) return Elsewhere(it, center);
        var nurse = map.NPCs.FirstOrDefault(n => n.IsHealingNurse);
        if (nurse == null) return Failed(it, "no nurse") ? ActionFrame.None : ActionFrame.None;
        return ToSpeak(it, nurse);
    }

    // To and fro over a stretch until the lead is of the level, to the nurse whenever it is low
    private ActionFrame Train(Itinerary it, Leg leg)
    {
        var team = game.Party.Members.Where(p => !p.IsEgg).ToList();
        var lead = team.FirstOrDefault(p => !p.IsFainted) ?? team.FirstOrDefault();
        if (lead == null) return Failed(it, "no team") ? ActionFrame.None : ActionFrame.None;
        if (lead.Level >= leg.Level)
        {
            Next(it);
            return ActionFrame.None;
        }
        bool low = team.Any(p => p.IsFainted) || lead.CurrentHP < lead.MaxHP * TrainHealShare;
        if (low && leg.Center != null)
        {
            if (!healing) Forget();
            healing = true;
        }
        if (healing)
        {
            if (!low && game.Map.Name == leg.Center)
            {
                healing = false;
                Forget();
            }
            else return leg.Center != null ? ToNurse(it, leg.Center) : ActionFrame.None;
        }
        var player = game.Player;
        if (game.Map.Name == leg.Map)
        {
            if (player.GridX == leg.X && player.GridY == leg.Y && !trainToSecond) { trainToSecond = true; Forget(); }
            else if (player.GridX == leg.X2 && player.GridY == leg.Y2 && trainToSecond) { trainToSecond = false; Forget(); }
        }
        int tx = trainToSecond ? leg.X2 : leg.X, ty = trainToSecond ? leg.Y2 : leg.Y;
        if (healingLevel != lead.Level) { healingLevel = lead.Level; it.Tries = 0; }
        return Toward(it, leg.Map, (x, y) => x == tx && y == ty, tx, ty, null);
    }

    // Toward another map, aimed at where on it the current leg is going (a trigger, a person, the nurse), so that of a
    // cave's two ways out the one on the right side is taken
    private ActionFrame Elsewhere(Itinerary it, string targetMap)
    {
        var leg = it.Current!;
        var there = MapDatabase.Get(targetMap);
        (int X, int Y) aim = (0, 0);
        if (leg.Kind is LegKind.Heal || leg.Kind == LegKind.Train && targetMap == leg.Center)
        {
            if (there.Everyone.FirstOrDefault(n => n.IsHealingNurse) is { } nurse) aim = (nurse.GridX, nurse.GridY);
        }
        else if (leg.Kind == LegKind.Trigger && there.Triggers.FirstOrDefault(t => t.Script == leg.What && (leg.Place == null || t.ScriptFile == leg.Place)) is { } t)
            aim = (t.X, t.Y);
        else if (leg.Kind == LegKind.Talk && there.Everyone.FirstOrDefault(n => n.Key == leg.What
                     && (leg.Place == null || (n.ScriptFile ?? there.ScriptFileAt(n.GridX, n.GridY)) == leg.Place)) is { } who)
            aim = (who.GridX, who.GridY);
        else if (leg.Kind is LegKind.Reach or LegKind.Train)
            aim = (leg.X, leg.Y);
        return Toward(it, targetMap, (_, _) => false, aim.X, aim.Y, null);
    }

    // No way on this map to the goal: a way off it whose map leads back onto it nearer the goal (a cave or a gate between)
    private bool Detour(Map map, int px, int py, int tx, int ty, out Func<int, int, bool> goal, out (int X, int Y) aim)
    {
        int now = Math.Abs(tx - px) + Math.Abs(ty - py);
        var doors = new HashSet<(int X, int Y)>();
        aim = (px, py);
        int best = int.MaxValue;
        foreach (var warp in map.Warps)
        {
            if (warp.Shut || warp.TargetMap == map.Name || string.IsNullOrEmpty(warp.TargetMap)) continue;
            var other = MapDatabase.Get(warp.TargetMap);
            if (other == map) continue;
            int back = other.Warps.Where(w => w.TargetMap == map.Name && !w.Shut)
                .Select(w => Math.Abs(w.TargetX - tx) + Math.Abs(w.TargetY - ty)).DefaultIfEmpty(int.MaxValue).Min();
            if (back >= now - 4) continue;
            doors.Add((warp.SourceX, warp.SourceY));
            int d = Math.Abs(warp.SourceX - px) + Math.Abs(warp.SourceY - py);
            if (d < best) { best = d; aim = (warp.SourceX, warp.SourceY); }
        }
        goal = (x, y) => doors.Contains((x, y));
        return doors.Count > 0;
    }

    /// <summary>
    /// A step toward a goal on <paramref name="targetMap"/>: on another map, toward the warp that leads on to it; on
    /// it, along the route to the nearest tile <paramref name="goal"/> accepts, kept and searched again as needed.
    /// </summary>
    private ActionFrame Toward(Itinerary it, string targetMap, Func<int, int, bool> goal, int tx, int ty, StepTrigger? mine, Action? onLastStep = null)
    {
        var map = game.Map;
        var player = game.Player;
        int px = player.GridX, py = player.GridY;

        if (map.Name != targetMap)
        {
            if (!nextMaps.TryGetValue((map.Name, targetMap), out string? next)) nextMaps[(map.Name, targetMap)] = next = Itinerary.NextMap(map.Name, targetMap, MapDatabase.Get);
            if (next == null) return Failed(it, $"no way from {map.Name} to {targetMap}") ? ActionFrame.None : ActionFrame.None;
            var ways = map.Warps.Where(w => w.TargetMap == next && !w.Shut && (w.SourceX, w.SourceY) != (px, py)).ToList();
            if (ways.Count == 0) return Failed(it, $"no way to {next}") ? ActionFrame.None : ActionFrame.None;
            // Of several ways to the next map, the one that comes out nearest where the leg is going, or else the nearest
            var aim = next == targetMap && (tx, ty) != (0, 0)
                ? ways.OrderBy(w => Math.Abs(w.TargetX - tx) + Math.Abs(w.TargetY - ty)).First()
                : ways.OrderBy(w => Math.Abs(w.SourceX - px) + Math.Abs(w.SourceY - py)).First();
            var doors = ways.Where(w => w.TargetX == aim.TargetX && w.TargetY == aim.TargetY || Math.Abs(w.SourceX - aim.SourceX) + Math.Abs(w.SourceY - aim.SourceY) <= 2)
                .Select(w => (w.SourceX, w.SourceY)).ToHashSet();
            goal = (x, y) => doors.Contains((x, y));
            (tx, ty) = (aim.SourceX, aim.SourceY);
            mine = null;
            onLastStep = null;
        }

        // Standing still against something, or off the route: searched again
        var here = (map.Name, px, py);
        if (here == lastTile) stillSeconds += 1f / 60f;
        else { stillSeconds = 0f; lastTile = here; }
        int at = routeTiles == null || routeMap != map.Name ? -1 : routeTiles.IndexOf((px, py));
        if (at < 0 || at >= route!.Count || stillSeconds > 1.5f)
        {
            if (stillSeconds > 1.5f && ++reroutes > 6)
            {
                stillSeconds = 0f;
                return Failed(it, $"stuck at {map.Name} {px},{py}") ? ActionFrame.None : ActionFrame.None;
            }
            stillSeconds = 0f;
            detoured = false;
            if (!Plan(map, px, py, goal, tx, ty, mine)
                && !(detoured = Detour(map, px, py, tx, ty, out var through, out var door) && Plan(map, px, py, through, door.X, door.Y, null)))
                return Failed(it, $"no route on {map.Name} from {px},{py} toward {tx},{ty}") ? ActionFrame.None : ActionFrame.None;
            at = 0;
            if (route!.Count == 0) return ActionFrame.None;
        }

        var way = route![at];
        if (at == route.Count - 1 && player.Facing == way && !detoured) onLastStep?.Invoke();
        engine.Steering = (way, player.HasRunningShoes);
        return ActionFrame.None;
    }

    private bool Plan(Map map, int x, int y, Func<int, int, bool> goal, int tx, int ty, StepTrigger? mine)
    {
        var player = game.Player;
        var walker = new Walker(player.Mode, player.HeightOn(map), false, player.FastGear, player.Moves, false, player.Bike);
        var armed = map.Triggers.Where(t => t != mine && Armed(t) && t.X + t.Width > Math.Min(x, tx) - 200 && t.X < Math.Max(x, tx) + 200
            && t.Y + t.Depth > Math.Min(y, ty) - 200 && t.Y < Math.Max(y, ty) + 200).ToList();
        bool Avoid(int ax, int ay)
        {
            foreach (var t in armed) if (t.Covers(ax, ay)) return true;
            return false;
        }
        var steps = Policy.Route(map, x, y, walker, goal, tx, ty, Avoid) ?? Policy.Route(map, x, y, walker, goal, tx, ty);
        if (steps == null) return false;
        route = steps;
        routeMap = map.Name;
        routeTiles = new List<(int X, int Y)>(steps.Count) { (x, y) };
        float h = walker.Height;
        var mode = walker.Mode;
        int cx = x, cy = y;
        foreach (var step in steps.Take(steps.Count - 1))
        {
            var s = FieldMovement.Step(map, cx, cy, step, walker with { Height = h, Mode = mode });
            (cx, cy, h, mode) = (s.X, s.Y, s.Height, s.Mode);
            routeTiles.Add((cx, cy));
        }
        return true;
    }
}
