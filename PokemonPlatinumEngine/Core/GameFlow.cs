using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>What a frame of a transition came to (<see cref="GameFlow.Advance"/>).</summary>
public enum TransitionStep
{
    /// <summary>No transition is under way.</summary>
    None,

    /// <summary>The picture is still closing over the state being left.</summary>
    Closing,

    /// <summary>The picture shut this frame: the midpoint's callback has run, and the next state is opening.</summary>
    Shut,

    /// <summary>The picture is still opening on the next state.</summary>
    Opening,

    /// <summary>The picture opened this frame: the next state is the game's.</summary>
    Opened
}

/// <summary>
/// The game's states without the renderers (plan 16 · T11), the way plan 06 · R2 split <c>BattleCore</c> from
/// <c>BattleEngine</c>: GPU-free, so a test drives it with no window. It is moved out of <see cref="GameEngine"/> state
/// by state, in place, nothing drawn differently at any step. Its first part is the frame every state hangs on: which
/// state the game is in, a transition from one to another (its clock, its way and the callback at its midpoint), where
/// the menus go back to, and the evolutions waiting to be played. The engine keeps the renderers, the window, the keys,
/// the settings and the sound, asks the flow each frame and draws what it says.
/// </summary>
public sealed partial class GameFlow
{
    /// <summary>The state the game is in now.</summary>
    public GameState State { get; set; } = GameState.Title;

    /// <summary>The state a transition closes over.</summary>
    public GameState Before { get; private set; } = GameState.Overworld;

    /// <summary>The state a transition opens on. A transition under way may be sent elsewhere (the Journal on a game taken up).</summary>
    public GameState After { get; set; } = GameState.Overworld;

    /// <summary>The way the transition under way closes and opens (<see cref="SceneTransition"/>).</summary>
    public TransitionKind Kind { get; private set; } = TransitionKind.Fade;

    /// <summary>The seconds since the transition's closing, or its opening, began: the clock's time, never the field's.</summary>
    public float Clock { get; private set; }

    /// <summary>Whether the transition is closing over the state being left (true) or opening on the next (false).</summary>
    public bool Closing { get; private set; } = true;

    private Action? midpoint;

    /// <summary>The state the close button's question was asked over, which is drawn under it and comes back on KEEP PLAYING (plan 12 · Q1).</summary>
    public GameState ClosingFrom { get; set; } = GameState.Title;

    /// <summary>Where the options screen goes back to: the title or the field.</summary>
    public GameState OptionsReturn { get; set; } = GameState.Overworld;

    /// <summary>Where the game goes once the last evolution waiting has been played.</summary>
    public GameState EvolutionReturn { get; set; } = GameState.Overworld;

    /// <summary>Evolutions waiting to be played one after the other.</summary>
    public Queue<EvolutionRequest> Evolutions { get; } = new();

    /// <summary>
    /// A Pokémon's model the flow wants built before the picture closes on it (a battle's, an evolution's, a hatching's,
    /// a walk's): the engine answers with <c>PokemonModels.Request</c>, so the flow asks for pictures without making any.
    /// </summary>
    public event Action<string>? ModelWanted;

    /// <summary>Says that the model of <paramref name="name"/> (a species or a form) is wanted soon.</summary>
    public void WantModel(string name) => ModelWanted?.Invoke(name);

    /// <summary>The state whose screen is shown: the one under the close button's question while it is asked.</summary>
    public GameState Shown => State == GameState.WindowClosing ? ClosingFrom : State;

    /// <summary>The scene the picture holds: during a transition the state being left while it closes and the next one while it opens.</summary>
    public GameState Scene => Shown == GameState.Transition ? (Closing ? Before : After) : Shown;

    /// <summary>
    /// Closes over the state the game is in and opens on <paramref name="next"/>, calling <paramref name="onMidpoint"/>
    /// while the picture is shut (where a map is loaded or a battle made).
    /// </summary>
    public void StartTransition(GameState next, Action? onMidpoint = null, TransitionKind kind = TransitionKind.Fade)
    {
        Before = State;
        After = next;
        midpoint = onMidpoint;
        State = GameState.Transition;
        Kind = kind;
        Clock = 0f;
        Closing = true;
    }

    /// <summary>Opens on <paramref name="state"/> from a picture already shut (the black the title leaves behind), with nothing at a midpoint.</summary>
    public void OpenOn(GameState state)
    {
        Before = state;
        After = state;
        midpoint = null;
        State = GameState.Transition;
        Closing = false;
        Clock = 0f;
    }

    /// <summary>
    /// Moves the transition under way on by <paramref name="dt"/> seconds. As the picture shuts, <paramref name="atShut"/>
    /// runs first (whatever the engine lets go of under the dark) and then the midpoint's callback; once it has
    /// opened, the next state is the game's.
    /// </summary>
    public TransitionStep Advance(float dt, Action? atShut = null)
    {
        if (State != GameState.Transition) return TransitionStep.None;
        Clock += dt;
        if (Closing)
        {
            if (Clock < SceneTransition.OutSeconds(Kind)) return TransitionStep.Closing;
            Closing = false;
            Clock = 0f;
            atShut?.Invoke();
            midpoint?.Invoke();
            midpoint = null;
            return TransitionStep.Shut;
        }
        if (Clock < SceneTransition.InSeconds(Kind)) return TransitionStep.Opening;
        State = After;
        return TransitionStep.Opened;
    }

    /// <summary>Asks the close button's question over the state the game is in.</summary>
    public void AskToClose()
    {
        ClosingFrom = State;
        State = GameState.WindowClosing;
    }

    /// <summary>Goes back to the state the close button's question was asked over.</summary>
    public void KeepPlaying() => State = ClosingFrom;
}
