using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core.Autoplay;

/// <summary>What feeds the game a stream of actions in place of the bot's decisions (plan 16 · T7, T13).</summary>
public interface IFuzzer
{
    /// <summary>The next frame's actions.</summary>
    ActionFrame Next();
}

/// <summary>
/// The field fuzzer (plan 16 · T13), GPU-free: a seeded random walk with random actions among its steps. A way is held
/// for a few tiles (running half the time), and now and then something else is pressed instead: A on whatever is
/// ahead, B, the start menu, the registered item, the Pokétch's buttons or a shoulder, each for a moment. Where the
/// key fuzzer mashes every button alike and so mostly stands in menus, this one goes places; <see cref="Spots"/>
/// chooses where on the open world it is set down.
/// </summary>
public sealed class FieldFuzzer : IFuzzer
{
    private static readonly GameAction[] Ways = { GameAction.Up, GameAction.Down, GameAction.Left, GameAction.Right };

    private static readonly GameAction[] Others =
    {
        GameAction.Confirm, GameAction.Confirm, GameAction.Confirm, GameAction.Confirm, GameAction.Cancel, GameAction.Cancel, GameAction.Menu,
        GameAction.Item, GameAction.Poketch, GameAction.PoketchApp, GameAction.PoketchTouch, GameAction.PageLeft, GameAction.PageRight
    };

    private readonly Random random;
    private readonly bool stick;
    private ActionFrame frame = ActionFrame.None;
    private GameAction[] held = Array.Empty<GameAction>();
    private (sbyte X, sbyte Y) tilt;
    private int holdFor;

    public FieldFuzzer(int seed) => random = new Random(seed);

    /// <summary>
    /// A fuzzer that steers a stick (plan 30 · FM9), for a region that moves freely: a way is an angle of sixteen and a
    /// tilt of a quarter to full, drawn from the seed, handed over as the frame's tilt with the direction bits the stick's
    /// eight ways set. With <paramref name="stick"/> false it is the grid's fuzzer, draw for draw.
    /// </summary>
    public FieldFuzzer(int seed, bool stick) : this(seed) => this.stick = stick;

    public ActionFrame Next()
    {
        if (holdFor-- <= 0)
        {
            int roll = random.Next(10);
            if (roll < 6)
            {
                // A way, for one to four tiles' worth of frames, sometimes running
                if (stick)
                {
                    double angle = random.Next(16) * Math.PI / 8;
                    float share = random.Next(2, 9) / 8f;
                    tilt = ((sbyte)Math.Round(Math.Sin(angle) * share * 127), (sbyte)Math.Round(Math.Cos(angle) * share * 127));
                    held = random.Next(2) == 0 ? new[] { GameAction.Run } : Array.Empty<GameAction>();
                    holdFor = random.Next(8, 8 + 16 * 4);
                    frame = Stir(frame, held, tilt);
                    return frame;
                }
                var way = Ways[random.Next(Ways.Length)];
                held = random.Next(2) == 0 ? new[] { way, GameAction.Run } : new[] { way };
                holdFor = random.Next(8, 8 + 16 * 4);
            }
            else if (roll < 9)
            {
                // Something else, pressed for a moment, and let go
                tilt = (0, 0);
                held = new[] { Others[random.Next(Others.Length)] };
                holdFor = random.Next(2, 6);
            }
            else
            {
                tilt = (0, 0);
                held = Array.Empty<GameAction>();
                holdFor = random.Next(4, 30);
            }
        }
        frame = stick ? Stir(frame, held, tilt) : frame.Next(held);
        return frame;
    }

    // The frame after another with actions held and a stick's tilt: its eight ways' bits added to what is held
    private static ActionFrame Stir(ActionFrame before, GameAction[] held, (sbyte X, sbyte Y) tilt)
    {
        var bits = ActionFrame.Stick(tilt.X, tilt.Y);
        var next = before.Next(held);
        uint down = next.Down | bits.Down;
        return new ActionFrame(down, down & ~before.Down) { MoveX = tilt.X, MoveY = tilt.Y };
    }

    /// <summary>
    /// Where on a map the fuzzer is set down: <paramref name="count"/> tiles drawn by a seed of their own, each open to
    /// walk on and, on a map of the world, in an area that is open, with nobody and no way or trigger on it.
    /// </summary>
    public static List<(int X, int Y)> Spots(Map map, int count, int seed)
    {
        var random = new Random(seed);
        var spots = new List<(int X, int Y)>();
        for (int tries = 0; spots.Count < count && tries < count * 20000; tries++)
        {
            int x = random.Next(map.Width), y = random.Next(map.Height);
            if (!map.IsWalkable(x, y) || map.GetWarpAt(x, y) != null || map.GetNpcAt(x, y) != null || map.IsDeepWater(x, y)) continue;
            if (map.IsStreamed && map.AreaAt(x, y) is not { Open: true }) continue;
            bool trigger = false;
            foreach (var t in map.TriggersAt(x, y)) { trigger = true; break; }
            if (trigger) continue;
            spots.Add((x, y));
        }
        return spots;
    }

    /// <summary>
    /// Spots over many maps (plan 16 · T13's leftover: the rooms, the caves and the other regions, beyond Sinnoh's
    /// overworld): the maps taken in an order drawn by the seed, one spot on each (<see cref="Spots"/>), until there
    /// are <paramref name="count"/>; a map with no spot to give is passed over.
    /// </summary>
    public static List<(Map Map, int X, int Y)> Places(IEnumerable<Map> maps, int count, int seed)
    {
        var random = new Random(seed);
        var order = new List<Map>(maps);
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        var places = new List<(Map, int, int)>();
        for (int i = 0; i < order.Count && places.Count < count; i++)
        {
            var spot = Spots(order[i], 1, seed * 7919 + i);
            if (spot.Count > 0) places.Add((order[i], spot[0].X, spot[0].Y));
        }
        return places;
    }
}
