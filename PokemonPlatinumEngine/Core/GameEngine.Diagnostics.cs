using System.Collections.Generic;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Graphics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

// What the game tells the log and a crash report of itself, and the wait behind another window (plan 16 · T3)
public partial class GameEngine
{
    // ---------------------------------------------------------------- waiting behind another window

    /// <summary>
    /// Whether the game's window is the one in front. <c>Program.Main</c> says so every frame; nobody else does, and
    /// the engine never asks raylib itself: what the system says of a window nobody sees is no answer (the screenshot
    /// harness's is hidden), and a tool's run must not stand still, or not, by it for having the option on.
    /// </summary>
    public bool WindowFocused { get; set; } = true;

    /// <summary>
    /// Whether the game is waiting for the player to come back (the options' "Pause in the background", off unless
    /// chosen): nothing is updated, so the play time, the field's life, a line being written and everything else
    /// stand where they were, and the sound drops (<see cref="AudioManager.Away"/>). The picture is still drawn. The
    /// close button's question is never kept waiting: whoever asked it is there to answer.
    /// </summary>
    public bool HeldForFocus => WaitsForFocus(Settings.FocusPause, WindowFocused, currentState);

    // Whether the last frame waited, so the log is told once when the wait begins and once when it ends
    private bool wasHeldForFocus;

    /// <summary>The rule of <see cref="HeldForFocus"/>: the option on, the window not in front, and no close button's question up.</summary>
    public static bool WaitsForFocus(bool pauseInBackground, bool windowFocused, GameState state) =>
        pauseInBackground && !windowFocused && state != GameState.WindowClosing;

    // ---------------------------------------------------------------- the first arrival in a place, timed

    // The places come to so far this run, and the arrival being timed: from the moment the player is put there to
    // the end of the first frame drawn there, which is when its ground has been baked and its people made ready
    private readonly HashSet<string> placesArrivedIn = new();
    private LogBook.PhaseScope? arrivalPhase;

    /// <summary>The player has come to a place: the first time this run, how long that takes goes to the log.</summary>
    private void TimeTheArrival()
    {
        if (currentMap == null || player == null) return;
        string place = PlaceKey();
        if (!placesArrivedIn.Add(place) || arrivalPhase != null) return;
        arrivalPhase = Log.Phase($"first arrival in {place}");
    }

    /// <summary>A frame has been drawn: an arrival being timed has come to its end.</summary>
    private void EndTheArrival()
    {
        arrivalPhase?.Dispose();
        arrivalPhase = null;
    }

    // ---------------------------------------------------------------- what a crash report says of the game

    /// <summary>
    /// The game as a crash report tells of it: the screen it is on (under the close button's question, the one the
    /// question was asked over), where the player stands if a game is in progress, the graphics card's name and the
    /// options in force.
    /// </summary>
    internal GameFacts DescribeForCrash()
    {
        string state = currentState == GameState.WindowClosing ? $"{closingFrom} (the close button's question was up)" : currentState.ToString();
        if (currentState == GameState.Transition) state = $"Transition ({stateBeforeTransition} to {stateAfterTransition})";
        string? renderer = Raylib.IsWindowReady() ? Gl.Renderer() : null;
        if (!gameStarted || currentMap == null || player == null) return new GameFacts(state, Renderer: renderer, Settings: Settings.Describe());
        return new GameFacts(state, currentMap.Name, currentMap.AreaAt(player.GridX, player.GridY)?.Key, player.GridX, player.GridY, renderer, Settings.Describe(),
            SavePaths.SlotName(saveSlot));
    }

    /// <summary>
    /// The game as it stands, for the crash save (<see cref="CrashReport.SaveName"/>); null when no game is in
    /// progress. It is whatever <see cref="BuildSave"/> gives in the middle of whatever was happening, which is why it
    /// is never written over the player's own save.
    /// </summary>
    internal SaveData? CrashSave() => gameStarted && currentMap != null && player != null ? BuildSave() : null;
}
