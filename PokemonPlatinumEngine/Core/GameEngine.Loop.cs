using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The loop catch in the game (plan 44 · SD10; style guide, "The loop catch"): a script's <c>loop go</c> opens the
/// station's screen on a task's Pokémon, the disc moved by the arrows, the stick or the mouse, and the script hears 1
/// as it is left with the Pokémon calmed and 0 otherwise. Nothing of the player's is in it, so nothing else of the game
/// changes. A game with no window plays the try at once by the autopilot.
/// </summary>
public partial class GameEngine
{
    /// <summary>A try at a task begun; false where there is no such task. The seed is drawn from <see cref="Dice"/>, or a tool's.</summary>
    internal bool OpenLoop(string taskId, uint? seed = null)
    {
        flow.ScriptAnswer = 0;
        if (LoopTasks.Get(taskId) is not { } task) return false;
        var game = new LoopCatch(task, seed ?? (uint)Dice.New().Next(), Settings.TimingAssist);
        if (IsHeadless)
        {
            flow.ScriptAnswer = LoopAutopilot.Play(game) == LoopOutcome.Calmed ? 1 : 0;
            return true;
        }
        flow.LoopScreen.Open(game);
        flow.State = GameState.Loop;
        return true;
    }

    private void UpdateLoop(float dt)
    {
        var screen = flow.LoopScreen;
        if (InputManager.IsActionPressed(GameAction.Cancel)) screen.Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) screen.Confirm();

        // The mouse has the disc while it moves or its button is down; the arrows and the stick have it otherwise
        var stick = InputManager.GetStick();
        var mouse = pointerNow;
        if (stick != System.Numerics.Vector2.Zero) loopByMouse = false;
        else if (mouse.Moved || mouse.Held) loopByMouse = true;
        var input = new LoopInput(stick, loopByMouse ? mouse.At : null,
            InputManager.IsActionDown(GameAction.Confirm) || mouse is { Held: true, Inside: true });
        screen.Advance(dt, input);
        HearTheLoop(screen);

        if (!screen.IsActive)
        {
            flow.ScriptAnswer = screen.Calmed ? 1 : 0;
            loopByMouse = false;
            flow.State = GameState.Overworld;
        }
    }

    private bool loopByMouse;

    private static void HearTheLoop(LoopScreen screen)
    {
        var happened = screen.Happened;
        if (happened == LoopEvents.None || screen.Game is not { } game) return;
        if ((happened & LoopEvents.Calmed) != 0) AudioManager.PlayCry(PokemonDatabase.Get(game.Task.Species)!);
        else if ((happened & LoopEvents.Looped) != 0) AudioManager.PlaySound("dowsing_ping");
        if ((happened & LoopEvents.Broke) != 0) AudioManager.PlaySound("bump");
        else if ((happened & LoopEvents.WindUp) != 0) AudioManager.PlaySound("exclaim");
    }

    private void DrawLoop() => ModernUi.DrawLoop(VirtualWidth, VirtualHeight, flow.LoopScreen);

    internal sealed partial class Driver
    {
        /// <summary>The loop catch's screen (plan 44 · SD10), and a try opened on a tool's seed.</summary>
        public LoopScreen LoopScreen => game.flow.LoopScreen;
        public bool OpenLoop(string task, uint seed) => game.OpenLoop(task, seed);
    }
}
