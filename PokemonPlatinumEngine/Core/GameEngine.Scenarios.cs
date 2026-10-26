using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// What a written scenario asks of the engine (plan 16 · T17): to begin again as if it had just been made, so a
/// scenario served after another by a warm harness draws what it draws run alone; and the few of the engine's own
/// parts a scenario's lines reach through the <see cref="Driver"/>.
/// </summary>
public partial class GameEngine
{
    /// <summary>
    /// The engine's own chance drawn again from <see cref="Dice"/> (which the caller has seeded), and nothing kept of
    /// what the field drew before: every scene and chunk let go, the field's life and its clock at nought, the camera
    /// at rest. The maps are the caller's to rebuild (<see cref="Data.MapDatabase.Initialize"/>), and the game its to begin.
    /// </summary>
    private void Afresh()
    {
        fieldRandom = Dice.New();
        peopleRandom = null;
        Steering = null;
        world.ForgetAll();
        world.Life.Restart();
        world.Camera.Reset();
    }

    internal sealed partial class Driver
    {
        /// <summary>The engine as it was made, for a scenario's <c>newgame</c> or <c>load</c> (plan 16 · T17).</summary>
        public void Afresh() => game.Afresh();

        /// <summary>The game's options, which a scenario's <c>time</c> changes and its next game puts back.</summary>
        public GameSettings Settings => game.Settings;

        public void ApplySettings() => game.ApplySettings(window: false);

        /// <summary>A question's answers on screen (a script's <c>ask</c> or <c>choose</c>).</summary>
        public ChoiceBox Choice => game.flow.Choice;

        public bool ScriptRunning => game.ScriptRunning;

        /// <summary>The player walked by a tool in place of the keys (<see cref="GameEngine.Steering"/>).</summary>
        public (Direction? Want, bool Run)? Steering { get => game.Steering; set => game.Steering = value; }
    }
}
