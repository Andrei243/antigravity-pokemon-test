using System;
using System.Linq;

namespace PokemonPlatinumEngine.Core.Autoplay;

/// <summary>
/// The key fuzzer (plan 16 · T7), GPU-free: a seeded stream of the game's actions, a frame at a time, as a player
/// mashing the pad would make them (a few held for a while, a press now and then). Full screen, mute and the
/// screenshot are left out: none of them changes the game, and the first two would touch a window.
/// </summary>
public sealed class KeyFuzzer : IFuzzer
{
    private static readonly GameAction[] Actions = Enum.GetValues<GameAction>()
        .Where(a => a is not (GameAction.Fullscreen or GameAction.Mute or GameAction.Screenshot or GameAction.Report or GameAction.Photo)).ToArray();

    private readonly Random random;
    private ActionFrame frame = ActionFrame.None;
    private GameAction[] held = Array.Empty<GameAction>();
    private int holdFor;

    public KeyFuzzer(int seed) => random = new Random(seed);

    /// <summary>The next frame's actions.</summary>
    public ActionFrame Next()
    {
        if (holdFor-- <= 0)
        {
            // Most often one action, sometimes none or two, held for a few frames to most of a second
            int count = random.Next(10) switch { 0 => 0, 9 => 2, _ => 1 };
            held = Enumerable.Range(0, count).Select(_ => Actions[random.Next(Actions.Length)]).Distinct().ToArray();
            holdFor = random.Next(2, 40);
        }
        frame = frame.Next(held);
        return frame;
    }
}
